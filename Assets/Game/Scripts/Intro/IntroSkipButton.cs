namespace NovaCore
{
    /// <summary>Botón "OMITIR INTRO" bajo la pantalla del video: gatillo / clic para saltarlo.</summary>
    public class IntroSkipButton : NovaInteractable
    {
        public IntroVideoPlayer player;
        public override bool AllowPoke => true;   // se puede pulsar tocándolo con el dedo

        public override string GetPrompt() => "Omitir intro";
        public override void OnUsePressed(NovaInteractor interactor) { if (player != null) player.Skip(); }
    }
}
