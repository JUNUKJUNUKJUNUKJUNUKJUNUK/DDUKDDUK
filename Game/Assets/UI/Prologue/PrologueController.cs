using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DdukDduk.Prologue
{
    public sealed class PrologueController : MonoBehaviour
    {
        private bool acceptingInput;
        private bool transitioning;

        private void Update()
        {
            // 새로하기를 누른 입력이 프롤로그까지 한 번에 넘기지 않게 한다.
            if (!acceptingInput)
            {
                acceptingInput = true;
                return;
            }

            bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool touched = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            if (transitioning || (!clicked && !touched)) return;

            transitioning = true;
            SceneManager.LoadScene("Assets/Shop/Scenes/RiceCakeShop.unity");
        }
    }
}
