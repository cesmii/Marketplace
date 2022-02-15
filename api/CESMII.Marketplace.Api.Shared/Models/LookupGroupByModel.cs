using CESMII.Marketplace.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CESMII.Marketplace.Api.Shared.Models
{
   public class LookupGroupByModel : LookupTypeModel
    {
        public List<LookupItemFilterModel> Items { get; set; }
    }
}
