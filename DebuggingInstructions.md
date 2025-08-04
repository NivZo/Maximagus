# Debugging Instructions for Input System Issues

## Changes Made to Identify Root Cause:

### 1. ✅ Removed Legacy GameInputManager
- Disabled `GameInputManager` registration in `ServiceLocator.cs`
- Only the new input system should be active now

### 2. ✅ Removed Legacy Card Input Initialization
- Removed old `InitializeNewInputSystem()` method from `Card.cs`
- Cards now only initialize via the notification system from Main

### 3. ✅ Added Comprehensive Debugging

**In HandManager.cs:**
- Added logging when `HandlePlayCardsRequested` is called
- Added logging when `HandleDiscardCardsRequested` is called  
- Added detailed logging in `CanSubmitHand` showing:
  - ActionType (Play/Discard)
  - CurrentState (what game state we're in)
  - RemainingHands count
  - RemainingDiscards count
  - Final result (true/false)

**In InputToCommandMapper.cs:**
- Added logging when Space key is pressed and PublishCardsRequestedEvent is published
- Added logging when Delete/Backspace key is pressed and DiscardCardsRequestedEvent is published

## Testing Instructions:

### Step 1: Run the game and observe console output

**What to look for:**
1. No more "input mapper not available" messages (Issue 1 should be fixed)
2. Cards should show "Card input handler initialized for card X via notification"

### Step 2: Test the keyboard actions and observe logs

**Press Enter** → Should see game state transition

**Select some cards, then press Space:**
Look for this sequence in the logs:
```
[InputMapper] Space key pressed - publishing PlayCardsRequestedEvent
HandlePlayCardsRequested called - processing play cards request
CanSubmitHand check: ActionType=Play, CurrentState=SubmitPhaseState, RemainingHands=5, RemainingDiscards=5
CanSubmitHand result: true
```

**Select some cards, then press Delete:**
Look for this sequence:
```
[InputMapper] Delete/Backspace key pressed - publishing DiscardCardsRequestedEvent  
HandleDiscardCardsRequested called - processing discard cards request
CanSubmitHand check: ActionType=Discard, CurrentState=SubmitPhaseState, RemainingHands=4, RemainingDiscards=5
CanSubmitHand result: true
```

### Step 3: Identify the Problem

**If you still see "Cannot submit hand: no Play actions remaining" errors, check:**

1. **Is the event being called multiple times?**
   - You should see only ONE "HandlePlayCardsRequested called" per Space press
   - If you see it multiple times, there are duplicate event publishers

2. **What's the CurrentState when error occurs?**
   - Look at the CanSubmitHand logs
   - If CurrentState is NOT "SubmitPhaseState", that's the issue
   - If RemainingHands/RemainingDiscards are 0, that's the issue

3. **Are the remaining counts being decremented correctly?**
   - RemainingHands should start at 5, go to 4, 3, 2, 1, 0
   - RemainingDiscards should start at 5, go to 4, 3, 2, 1, 0

## Expected Results:
- ✅ Issue 1: No "input mapper not available" errors
- ✅ Issue 2: No false "no Play actions remaining" errors  
- ✅ Issue 3: No false "no Discard actions remaining" errors

## Next Steps:
Once you test this and see the console output, we'll know exactly what's causing the false error messages and can fix it precisely.

The debugging will show us:
1. Whether events are being published multiple times
2. Whether the game state is wrong when trying to submit
3. Whether the remaining action counts are being decremented incorrectly
4. Whether there are timing issues

**Please run the game and share what you see in the console logs!**