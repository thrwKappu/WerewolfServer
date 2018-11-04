using System;

namespace DNWS.Werewolf
{
    public class WerewolfException : Exception
    {
        public WerewolfException()
        {
        }
        public WerewolfException(string m) : base (m)
        {
        }
    }
    public class GameNotFoundWerewolfException : WerewolfException
    {
        public GameNotFoundWerewolfException()
        {
        }

        public GameNotFoundWerewolfException(string m) : base (m)
        {
        }
    }
    public class GameNotPlayableWerewolfException : WerewolfException
    {
        public GameNotPlayableWerewolfException(string m) : base (m)
        {
        }
    }
    public class PlayerInGameAlreadyWerewolfException : WerewolfException
    {
        public PlayerInGameAlreadyWerewolfException(string m) : base(m)
        {
        }
    }
    public class PlayerNotFoundWerewolfException : WerewolfException
    {
        public PlayerNotFoundWerewolfException(string m) : base (m)
        {
        }
        public PlayerNotFoundWerewolfException()
        {
        }
    }
    public class PlayerIsNotAliveWerewolfException : WerewolfException
    {
        public PlayerIsNotAliveWerewolfException()
        {
        }
        public PlayerIsNotAliveWerewolfException(string m) : base(m)
        {
        }
    }
    public class PlayerIsNotInGameWerewolfException : WerewolfException
    {
        public PlayerIsNotInGameWerewolfException()
        {
        }
        public PlayerIsNotInGameWerewolfException(string m) : base(m)
        {
        }
    }
    public class PlayerIsNotAllowToChatWerewolfException : WerewolfException
    {
        public PlayerIsNotAllowToChatWerewolfException()
        {
        }

        public PlayerIsNotAllowToChatWerewolfException(string m) : base(m)
        {
        }
    }
    public class DuplicatePlayerWerewolfException : WerewolfException
    {
        public DuplicatePlayerWerewolfException()
        {
        }
        public DuplicatePlayerWerewolfException(string m) : base(m)
        {
        }
    }
    public class ActionNotFoundWerewolfException : WerewolfException
    {
        public ActionNotFoundWerewolfException()
        {
        }
        public ActionNotFoundWerewolfException(string m) : base(m)
        {
        }
    }
    public class TargetNotFoundWerewolfException : WerewolfException
    {
        public TargetNotFoundWerewolfException()
        {
        }
        public TargetNotFoundWerewolfException(string m) : base(m)
        {
        }
    }
    public class CantPerformOnYourselfWerewolfException : WerewolfException
    {
        public CantPerformOnYourselfWerewolfException()
        {
        }

        public CantPerformOnYourselfWerewolfException(string m) : base(m)
        {
        }
    }
    public class ProcessingPeriodWerewolfException : WerewolfException
    {
        public ProcessingPeriodWerewolfException()
        {
        }

        public ProcessingPeriodWerewolfException(string m) : base(m)
        {
        }
    }
}