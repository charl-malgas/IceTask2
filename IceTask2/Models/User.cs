using System;
using System.Collections.Generic;
using System.Text;

namespace UserManagementFunctions.Models
{
    public class User
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public int Age { get; set; }
    }
}
