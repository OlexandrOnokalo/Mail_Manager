using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mail_Manager.Models
{
    public static class SessionState
    {
        public static string Email { get; set; } = string.Empty;
        public static string Password { get; set; } = string.Empty;
    }
}
