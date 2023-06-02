using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vk_comment_bot
{
    internal class ModelHistory
    {
        public string OwnerId { get; set; }
        public string Date { get; set; }
        public static bool operator ==(ModelHistory h1, ModelHistory h2)
        {
            if (ReferenceEquals(h1, h2))
                return true;

            if (ReferenceEquals(h1, null) || ReferenceEquals(h2, null))
                return false;

            return h1.OwnerId == h2.OwnerId && h1.Date == h2.Date;
        }

        public static bool operator !=(ModelHistory h1, ModelHistory h2)
        {
            return !(h1 == h2);
        }
    }
}
