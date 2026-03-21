using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class PlayerUI : MonoBehaviour
{
    public InputAction submitAction;
    public string currentInput = "";
    public UnityEvent<string> onInputChange;

    void OnEnable()
    {
        if (submitAction != null)
        {
            submitAction.Enable();
            submitAction.performed += OnSubmit;
        }

        if (Keyboard.current != null)
            Keyboard.current.onTextInput += OnTextInput;
    }

    void OnDisable()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput -= OnTextInput;

        if (submitAction != null)
        {
            submitAction.performed -= OnSubmit;
            submitAction.Disable();
        }
    }

    // Receives characters according to the active keyboard layout
    void OnTextInput(char c)
    {
        // ignore control keys (enter, return, tab)
        if (c < 32 && c != ' ')
            return;

        // backspace
        if (c == '\b')
        {
            if (currentInput.Length > 0)
            {
                currentInput = currentInput.Substring(0, currentInput.Length - 1);
                onInputChange?.Invoke(currentInput);
            }
            return;
        }

        // ignore space if needed remove this check
        if (c == '\r' || c == '\n')
            return;

        if (currentInput.Length < WordManager.Instance.currentPlanned.text.Length)
        {
            currentInput += c;
            onInputChange?.Invoke(currentInput);
        }
    }


    void Update()
    {
        var kb = Keyboard.current;

        if (kb != null)
        {
            // Fallback backspace handling in case onTextInput isn't firing for it
            if (kb.backspaceKey.wasPressedThisFrame && currentInput.Length > 0)
            {
                currentInput = currentInput.Substring(0, currentInput.Length - 1);
                onInputChange?.Invoke(currentInput);
            }

            // Fallback space handling in case onTextInput misses it on some setups
            if (kb.spaceKey.wasPressedThisFrame)
            {
                currentInput += " "; 
                onInputChange?.Invoke(currentInput);
            }

            // Fallback enter handling when submitAction is not assigned
            if ((submitAction == null || !submitAction.enabled) && kb.enterKey.wasPressedThisFrame)
            {
                SubmitAndClear();
            }
        }
    }

    void OnSubmit(InputAction.CallbackContext ctx)
    {
        SubmitAndClear();
    }

    void SubmitAndClear()
    {
        SubmitInput();
        currentInput = "";
        onInputChange?.Invoke(currentInput);
    }

    public void SubmitInput()
    {
        if (WordManager.Instance != null) WordManager.Instance.SubmitWord(currentInput);
    }
}
