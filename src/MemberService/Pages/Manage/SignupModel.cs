﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Clave.Expressionify;
using MemberService.Data;
using MemberService.Data.ValueTypes;

namespace MemberService.Pages.Manage
{
    public class SignupModel
    {
        public Guid EventId { get; private set; }

        public string Title { get; private set; }

        public Status Status { get; private set; }

        public DateTime SignedUpAt { get; private set; }

        public DanceRole? Role { get; private set; }

        public static SignupModel Create(EventSignup s)
        {
            return new SignupModel
            {
                EventId = s.EventId,
                Title = s.Event.Title,
                Status = s.Status,
                SignedUpAt = s.SignedUpAt,
                Role = s.Event.SignupOptions.RoleSignup ? (DanceRole?)s.Role : null
            };
        }
    }
}
