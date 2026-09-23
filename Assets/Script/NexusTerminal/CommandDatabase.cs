using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CommandDatabase : MonoBehaviour
{
    [Serializable]
    public sealed class CommandDefinition
    {
        [Tooltip("The command typed by the player, without the prompt prefix.")]
        public string command;

        [Tooltip("One-line description shown by the help command.")]
        public string description;

        [TextArea(3, 12)]
        [Tooltip("The response printed after the command is submitted.")]
        public string response;
    }

    [SerializeField]
    private List<CommandDefinition> commands = new List<CommandDefinition>();

    public IReadOnlyList<CommandDefinition> Commands => commands;

    private void Awake()
    {
        if (commands.Count == 0)
        {
            AddDefaultCommands();
        }
    }

    /// <summary>
    /// Finds a command by its first whitespace-delimited token.
    /// </summary>
    public bool TryGetCommand(string rawInput, out CommandDefinition definition)
    {
        string normalizedCommand = GetCommandName(rawInput);

        for (int i = 0; i < commands.Count; i++)
        {
            CommandDefinition candidate = commands[i];
            if (candidate != null && !string.IsNullOrWhiteSpace(candidate.command) && string.Equals(candidate.command.Trim(), normalizedCommand, StringComparison.OrdinalIgnoreCase))
            {
                definition = candidate;
                return true;
            }
        }

        definition = null;
        return false;
    }

    /// <summary>
    /// Builds the help output from the commands currently configured in the Inspector.
    /// </summary>
    public string BuildHelpResponse()
    {
        StringBuilder help = new StringBuilder();
        help.AppendLine("AVAILABLE COMMANDS:");
        help.AppendLine();

        for (int i = 0; i < commands.Count; i++)
        {
            CommandDefinition definition = commands[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.command))
            {
                continue;
            }

            help.Append(definition.command.Trim().PadRight(12));
            help.AppendLine((definition.description ?? string.Empty).Trim());
            help.AppendLine();
        }

        return help.ToString().TrimEnd();
    }

    /// <summary>
    /// Extracts and normalizes the command name from a complete input line.
    /// </summary>
    public static string GetCommandName(string rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return string.Empty;
        }

        string trimmedInput = rawInput.Trim();
        int separatorIndex = trimmedInput.IndexOfAny(new[] { ' ', '\t', '\r', '\n' });
        string commandName = separatorIndex >= 0
            ? trimmedInput.Substring(0, separatorIndex)
            : trimmedInput;

        return commandName.ToLowerInvariant();
    }

    /// <summary>
    /// Replaces the configured command list with the built-in NEXUS commands.
    /// </summary>
    [ContextMenu("Reset To Default Commands")]
    public void ResetToDefaultCommands()
    {
        commands.Clear();
        AddDefaultCommands();
    }

    private void AddDefaultCommands()
    {
        commands.Add(new CommandDefinition
        {
            command = "help",
            description = "Show available commands",
            response = string.Empty
        });
        commands.Add(new CommandDefinition
        {
            command = "status",
            description = "Show system status",
            response = "SYSTEM STATUS:\n\nREACTOR: CRITICAL\n\nPOWER: 37%\n\nSECURITY: ACTIVE\n\nROCKET: LOCKED\n\nTIME REMAINING: 03:00"
        });
        commands.Add(new CommandDefinition
        {
            command = "reactor",
            description = "Check reactor status and authorization clue",
            response = "REACTOR SYSTEM:\n\nCORE STATUS: UNSTABLE\n\nCOOLING SYSTEM: FAILING\n\nROCKET LINK: LOCKED\n\nAUTHORIZATION: REQUIRED\n\nEMERGENCY ROCKET REQUIRES REACTOR AUTHORIZATION.\nACCESS REACTOR FILE TO CONTINUE."
        });
        commands.Add(new CommandDefinition
        {
            command = "security",
            description = "View security logs",
            response = "SECURITY LOG:\n\n[OK] AUTHENTICATION CHANNEL SECURE\n[OK] PERIMETER SENSORS ONLINE\n[WARN] REACTOR AUTHORIZATION REQUIRED\n[OK] CREW ACCESS LEVEL: AUTHORIZED"
        });
        commands.Add(new CommandDefinition
        {
            command = "doors",
            description = "Control door systems",
            response = "DOOR CONTROL:\n\nMAIN AIRLOCK     SEALED\nENGINEERING      LOCKED\nCREW QUARTERS    SECURE\n\nUse the physical control panel to change door states."
        });
        commands.Add(new CommandDefinition
        {
            command = "clear",
            description = "Clear terminal",
            response = string.Empty
        });
    }
}
