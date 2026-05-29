using System.Collections.Generic;

namespace IT_Gied.Models
{
    public class AdminRoleViewModel
    {
        public bool AdminRoleExists { get; set; }
        public string? AdminRoleId { get; set; }
        public List<AdminRoleUserViewModel> Users { get; set; } = new List<AdminRoleUserViewModel>();
    }
}
