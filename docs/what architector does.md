1. define Core and Features as assembly definitions. things in Features can reference only core:   Assets/Scripts/
   Core/        ← Assembly Definition: "Core"
   Features/
   Robot/     ← Assembly Definition: "Robot", references Core only
   Cleaning/  ← Assembly Definition: "Cleaning", references Core only
   Question: how to make assembly definition and how to reference only core?
2. If there are different playable characters, Core gains a concept of a generic controlled actor with action slots(Move, Interact, Ability1 etc.,) , input routing (what is it? ) and 'which character is currently controlled'.
    Example:
        Features/Robot (movement profile, clean ability, modules etc)
        Features/Cat (different movement, like pounce, hide, etc)
        Each of them knows what they themselves do and how they react when they become or stop bein active
3. Switching control. either is is a third feature that writes to core state like controleld actor = X, or the Core state directly. I think it should be a third feature. In crimson desert the switching between actors happens by quest or clicking certain button, and it feels like a third feature. And it works from the UI
4. make walking skeleton? 
