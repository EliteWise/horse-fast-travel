using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace HorseFastTravel;

/// <summary>The native text-choice dialogue, with one response and safe dismissal.</summary>
internal sealed class TravelMenu : DialogueBox
{
    private readonly GameLocation source;
    private readonly GameLocation.afterQuestionBehavior answerHandler;

    public TravelMenu(string question, Response[] responses, GameLocation source)
        : base(question, responses)
    {
        this.source = source;
        this.answerHandler = source.afterQuestion;
        this.PositionCloseButton();
    }

    private void PositionCloseButton()
    {
        this.xPositionOnScreen = this.x;
        this.yPositionOnScreen = this.y - (this.heightForQuestions - this.height);
        this.initializeUpperRightCloseButton();
    }

    private void Dismiss()
    {
        if (this.transitioning || Game1.activeClickableMenu != this)
            return;
        if (this.source.afterQuestion == this.answerHandler)
            this.source.afterQuestion = null;
        this.selectedResponse = -1;
        this.beginOutro();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (this.upperRightCloseButton.containsPoint(x, y))
            this.Dismiss();
        else
            base.receiveLeftClick(x, y, playSound);
    }

    public override void receiveKeyPress(Keys key)
    {
        // Native questions answer their last response on Escape/N. Never let dismissal
        // select the sole travel response; keep all other native choice handling.
        if (key is Keys.Escape or Keys.N || Game1.options.doesInputListContain(Game1.options.menuButton, key))
            this.Dismiss();
        else
            base.receiveKeyPress(key);
    }

    public override void receiveGamePadButton(Buttons button)
    {
        if (button == Buttons.B)
            this.Dismiss();
        else
            base.receiveGamePadButton(button);
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        this.PositionCloseButton();
    }

    public override void draw(SpriteBatch b)
    {
        base.draw(b);
        if (!this.transitioning)
        {
            this.upperRightCloseButton.draw(b);
            this.drawMouse(b);
        }
    }
}
