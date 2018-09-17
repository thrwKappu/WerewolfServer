
using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace DNWS.Werewolf 
{ 
    /// <summary>
    /// 
    /// </summary>
    public partial class Role
    { 
        public enum TypeEnum
        { 
            /// <summary>
            ///
            /// </summary>
            [EnumMember(Value = "Villager")]
            VillagerEnum = 1,
            
            /// <summary>
            ///
            /// </summary>
            [EnumMember(Value = "Wolf")]
            WolfEnum = 2,
            
            /// <summary>
            ///
            /// </summary>
            [EnumMember(Value = "Neutral")]
            NeutralEnum = 3,
        }
        [Required]
        [DataMember(Name="type")]
        public TypeEnum? Type { get; set; }

        public List<Role> Roles { get; set;}
    }
}