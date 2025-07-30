using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        // "Build Settings"에 등록된 게임 씬의 이름이나 번호를 넣습니다.
        // 씬 이름을 사용하는 것이 더 안전합니다.
        SceneManager.LoadScene("BasicScene"); // "YourGameSceneName"을 실제 게임 씬 파일 이름으로 바꾸세요!
    }

    public void QuitGame()
    {
        // 에디터에서는 작동하지 않지만, 빌드된 게임에서는 종료됩니다.
        Debug.Log("게임을 종료합니다.");
        Application.Quit();
    }
}
