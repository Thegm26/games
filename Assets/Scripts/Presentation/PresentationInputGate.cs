namespace WhoEnters.Presentation
{
    /// <summary>Pure input policy shared by buttons, swipes, and keyboard routes.</summary>
    public static class PresentationInputGate
    {
        public static bool IsDecisionSurfaceAvailable(bool cardVisible, bool buttonsVisible, bool overlayVisible)
            => cardVisible && buttonsVisible && !overlayVisible;

        /// <returns>True when the input was consumed by presentation and must not resolve a verdict.</returns>
        public static bool ConsumeDecisionCaption(CaptionSequenceController captions)
        {
            if (captions == null || !captions.IsActive) return false;
            var visitorCaption = captions.CurrentLine != null && captions.CurrentLine.Role == PresentationRole.Visitor;
            if (visitorCaption && captions.IsTextComplete)
                return captions.HandleAction() != TypewriterAction.CompletedSequence;

            captions.HandleAction();
            return true;
        }
    }
}
