namespace Fluent.Core.Diagnostics;

/// <summary>
/// Maps a dictation failure stage to a safe, non-technical French message with a
/// recovery hint. Pure and deterministic; contains no raw exception detail so it
/// never leaks technical or sensitive information to the user interface.
/// </summary>
public static class DictationErrorPresenter
{
    public static UserFacingMessage Describe(DictationFailureStage stage)
    {
        return stage switch
        {
            DictationFailureStage.Microphone => new UserFacingMessage(
                "La dictée n’a pas pu utiliser le microphone.",
                "Vérifiez que le micro est branché et autorisé, puis réessayez."),
            DictationFailureStage.Transcription => new UserFacingMessage(
                "La transcription locale n’a pas abouti.",
                "Réessayez ; au premier usage, laissez le modèle local finir de se préparer."),
            DictationFailureStage.Rewriting => new UserFacingMessage(
                "La réécriture a rencontré un problème.",
                "Votre texte local exact est conservé ; réessayez la dictée."),
            DictationFailureStage.Insertion => new UserFacingMessage(
                "Le texte n’a pas pu être inséré dans la cible.",
                "S’il est dans le presse-papiers, collez-le avec Ctrl+V ; sinon réessayez."),
            _ => new UserFacingMessage(
                "Une erreur est survenue pendant la dictée.",
                "Réessayez ; si le problème persiste, redémarrez Fluent."),
        };
    }
}
