﻿using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Throb.Data.Entities
{
    public class ApplicationUser:IdentityUser
    {
        public string  Firstname { get; set; }
        public string Lastname { get; set; }
        public bool IsActive { get; set; }
    }
}
