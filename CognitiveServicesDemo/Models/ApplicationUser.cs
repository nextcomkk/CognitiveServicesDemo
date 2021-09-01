using System.Collections.Generic;

using Microsoft.AspNetCore.Identity;

namespace CognitiveServicesDemo.Models
{
    public partial class ApplicationUser : IdentityUser
    {
        public ApplicationUser()
        {
            UserMedia = new HashSet<UserMedia>();
        }

        public virtual ICollection<UserMedia> UserMedia { get; set; }
    }
}

