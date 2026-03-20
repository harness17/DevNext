namespace Site.Common
{
    public class ImportValidator
    {
        private Dictionary<string, List<(Func<object?, bool> rule, string message, bool stopOnError)>> _rules = new();

        public void AddRules(string field, params (Func<object?, bool> rule, string message, bool stopOnError)[] rules)
        {
            if (!_rules.ContainsKey(field)) _rules[field] = new List<(Func<object?, bool>, string, bool)>();
            _rules[field].AddRange(rules);
        }

        public List<string> Validate(string field, object? value)
        {
            var errors = new List<string>();
            if (!_rules.ContainsKey(field)) return errors;
            foreach (var (rule, message, stopOnError) in _rules[field])
            {
                if (!rule(value))
                {
                    errors.Add(message);
                    if (stopOnError) break;
                }
            }
            return errors;
        }

        public static (Func<object?, bool>, string, bool) CreateRule(Func<object?, bool> ruleFunc, string message, bool stopOnError = false)
        {
            return (ruleFunc, message, stopOnError);
        }

        public static class Rules
        {
            public static Func<object?, bool> IsNotNull => v => v != null;
            public static Func<object?, bool> IsNotEmpty => v => v != null && v.ToString() != "";
            public static Func<object?, bool> IsInteger => v => v == null || int.TryParse(v?.ToString(), out _);
            public static Func<object?, bool> IsType<T>() => v => v is T;
        }
    }
}
