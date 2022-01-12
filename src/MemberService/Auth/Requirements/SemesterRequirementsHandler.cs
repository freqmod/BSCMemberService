﻿namespace MemberService.Auth.Requirements;

using System.Security.Claims;

using Clave.Expressionify;

using MemberService.Auth.Requirements;
using MemberService.Data;
using MemberService.Data.ValueTypes;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

using R = Data.SemesterRole.RoleType;

public class SemesterRequirementsHandler : IAuthorizationHandler
{
    private readonly MemberContext _database;
    private readonly UserManager<User> _userManager;

    public SemesterRequirementsHandler(MemberContext database, UserManager<User> userManager)
    {
        _database = database;
        _userManager = userManager;
    }

    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        var pendingRequirements = context.PendingRequirements.OfType<Requirement>().ToList();
        var user = context.User;

        if (!user.Identity.IsAuthenticated) return;

        var id = context.GetGuidResource();

        foreach (var requirement in pendingRequirements)
        {
            if (await IsAuthorized(user, id, requirement))
            {
                context.Succeed(requirement);
            }
        }
    }

    private async Task<bool> IsAuthorized(ClaimsPrincipal user, Guid? id, Requirement requirement)
        => requirement.Policy switch
        {
            Policy.CanCreateEvent => await CheckCurrentSemesterRole(user, R.Coordinator),

            Policy.CanViewEvent when id is Guid eventId => await CheckEventSemesterRole(eventId, user, R.ViewOnly, R.Instructor, R.Coordinator),
            Policy.CanEditEvent when id is Guid eventId => await CheckEventSemesterRole(eventId, user, R.Coordinator),
            Policy.CanSetEventSignupStatus when id is Guid eventId => await CheckEventSemesterRole(eventId, user, R.Coordinator),

            Policy.CanSetPresence when id is Guid eventId => await CheckEventSemesterRole(eventId, user, R.Instructor, R.Coordinator),
            Policy.CanAddPresenceLesson when id is Guid eventId => await CheckEventSemesterRole(eventId, user, R.Instructor, R.Coordinator),

            Policy.CanViewSurvey when id is Guid eventId => await CheckSemesterRole(eventId, user, R.ViewOnly, R.Instructor, R.Coordinator),

            Policy.CanViewMembers => await CheckCurrentSemesterRole(user, R.ViewOnly, R.Instructor, R.Coordinator),

            Policy.CanViewSemester when id is Guid semesterId => await CheckSemesterRole(semesterId, user, R.ViewOnly, R.Instructor, R.Coordinator),
            Policy.CanViewSemester => await CheckCurrentSemesterRole(user, R.ViewOnly, R.Instructor, R.Coordinator),
            _ => false,
        };

    private async Task<bool> CheckEventSemesterRole(Guid eventId, ClaimsPrincipal user, params SemesterRole.RoleType[] roleTypes)
    {        
        var @event = await _database.Events.FindAsync(eventId);

        if (@event?.SemesterId is not Guid semesterId) return false;

        return await CheckSemesterRole(semesterId, user, roleTypes);
    }

    private async Task<bool> CheckSemesterRole(Guid semesterId, ClaimsPrincipal user, params SemesterRole.RoleType[] roleTypes)
    {
        var userId = _userManager.GetUserId(user);
        var permissions = await _database.SemesterRoles.FindAsync(semesterId, userId);

        if (permissions is null) return false;

        return roleTypes.Contains(permissions.Role);
    }

    private async Task<bool> CheckCurrentSemesterRole(ClaimsPrincipal user, params SemesterRole.RoleType[] roleTypes)
    {
        var userId = _userManager.GetUserId(user);
        var semesterRole = await _database.Semesters
            .Expressionify()
            .Where(s => s.IsActive())
            .OrderByDescending(s => s.SignupOpensAt)
            .SelectMany(s => s.Roles.Where(r => r.UserId == userId))
            .FirstOrDefaultAsync();

        if (semesterRole is null) return false;

        return roleTypes.Contains(semesterRole.Role);
    }
}
