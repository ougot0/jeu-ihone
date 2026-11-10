namespace Arsenal.Core.Data
{
    /// <summary>Résultat d'une opération de jeu (achat, installation...) avec un code stable et un message lisible.</summary>
    public readonly struct OperationResult
    {
        public bool Success { get; }
        public string Code { get; }
        public string Message { get; }

        private OperationResult(bool success, string code, string message)
        {
            Success = success; Code = code; Message = message;
        }

        public static OperationResult Ok(string message = "") => new OperationResult(true, ResultCodes.Ok, message);
        public static OperationResult Fail(string code, string message) => new OperationResult(false, code, message);
        public override string ToString() => $"{Code}: {Message}";
    }

    public static class ResultCodes
    {
        public const string Ok = "ok";
        public const string NotFound = "not_found";
        public const string NotOwned = "not_owned";
        public const string AlreadyOwned = "already_owned";
        public const string InsufficientFunds = "insufficient_funds";
        public const string LevelTooLow = "level_too_low";
        public const string WrongType = "wrong_type";
        public const string WrongMount = "wrong_mount";
        public const string WrongFamily = "wrong_family";
        public const string Conflict = "conflict";
        public const string SlotRequired = "slot_required";
        public const string LimitReached = "limit_reached";
        public const string InvalidName = "invalid_name";
        public const string LastBuild = "last_build";
    }
}
