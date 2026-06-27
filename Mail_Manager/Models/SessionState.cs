using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mail_Manager.Models
{
    // замість DI — свідомо взяв static, бо ComposeWindow відкривається без параметрів і без DI-контейнера тут було б громіздко
    public static class SessionState
    {
        // заповнюються один раз у LoginWindow після успішної автентифікації, читаються з ComposeWindow при відправці
        public static string Email { get; set; } = string.Empty;
        public static string Password { get; set; } = string.Empty;
    }
}
