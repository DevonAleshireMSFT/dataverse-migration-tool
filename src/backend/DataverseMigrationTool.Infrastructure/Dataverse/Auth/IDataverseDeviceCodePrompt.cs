namespace DataverseMigrationTool.Infrastructure.Dataverse.Auth;

/// <summary>
/// Presents MSAL device-code prompts through a trusted application UX without logging device codes or tokens.
/// </summary>
public interface IDataverseDeviceCodePrompt
{
    /// <summary>
    /// Presents a device-code prompt to the current operator.
    /// </summary>
    /// <param name="context">The time-limited prompt values to display through a trusted channel.</param>
    /// <param name="cancellationToken">A token used to cancel the prompt.</param>
    /// <returns>A task that completes after the prompt has been presented.</returns>
    ValueTask ShowAsync(DataverseDeviceCodePromptContext context, CancellationToken cancellationToken);
}
