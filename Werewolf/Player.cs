/*
 * Werewolf Engine
 *
 * This is a werewolf game engine for REST access. It is primarily developed for CPE200 class at Computer Engineering, Chiang Mai University.
 *
 * OpenAPI spec version: 0.1.0
 * Contact: pruetboonma@gmail.com
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

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
using Newtonsoft.Json.Converters;

namespace DNWS.Werewolf 
{ 
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    [DataContract]
    public partial class Player
    { 
        /// <summary>
        /// Gets or Sets Id
        /// </summary>
        [DataMember(Name="id")]
        public long? Id { get; set; }

        /// <summary>
        /// Gets or Sets Name
        /// </summary>
        [Required]
        [DataMember(Name="name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or Sets Password
        /// </summary>
        [Required]
        [DataMember(Name="password")]
        public string Password { get; set; }

        /// <summary>
        /// Gets or Sets Game
        /// </summary>
        [DataMember(Name="game")]
        public Game Game { get; set; }

        [DataMember(Name="gameid")]
        public long? GameId { get; set; }

        /// <summary>
        /// Gets or Sets Role
        /// </summary>
        [DataMember(Name="role")]
        public Role Role { get; set; }

        /// <summary>
        /// Gets or Sets Session
        /// </summary>
        [DataMember(Name="session")]
        public string Session { get; set; }
        /// <summary>
        /// Player status in a game
        /// </summary>
        /// <value>Player status in a game</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum StatusEnum
        { 
            /// <summary>
            /// Enum AliveEnum for alive
            /// </summary>
            [EnumMember(Value = "alive")]
            AliveEnum = 1,
            
            /// <summary>
            /// Enum OfflineEnum for offline
            /// </summary>
            [EnumMember(Value = "offline")]
            OfflineEnum = 2,
            
            /// <summary>
            /// Enum NotInGameEnum for not in game
            /// </summary>
            [EnumMember(Value = "not in game")]
            NotInGameEnum = 3,
            
            /// <summary>
            /// Enum VoteDeadEnum for dead
            /// </summary>
            [EnumMember(Value = "votedead")]
            VoteDeadEnum = 4,

            /// <summary>
            /// Enum ShotDeadEnum for dead
            /// </summary>
            [EnumMember(Value = "shotdead")]
            ShotDeadEnum = 5,

            /// <summary>
            /// Enum JailDeadEnum for dead
            /// </summary>
            [EnumMember(Value = "jaildead")]
            JailDeadEnum = 6,

            /// <summary>
            /// Enum HolyDeadEnum for dead
            /// </summary>
            [EnumMember(Value = "holydead")]
            HolyDeadEnum = 7,

            /// <summary>
            /// Enum KillDeadEnum for dead
            /// </summary>
            [EnumMember(Value = "killdead")]
            KillDeadEnum = 8,
        }

        /// <summary>
        /// Player status in a game
        /// </summary>
        /// <value>Player status in a game</value>
        [Required]
        [DataMember(Name = "status")]
        public StatusEnum? Status { get; set; }
        

        /// <summary>
        /// Registeration Date
        /// </summary>
        /// <value>date in yyyy-mm-dd format</value>
        [DataMember(Name="regisdate")]
        public string Regisdate { get; set; }
    }
}
