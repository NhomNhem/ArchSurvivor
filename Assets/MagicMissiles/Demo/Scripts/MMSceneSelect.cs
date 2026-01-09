using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace MagicMissiles
{


public class MMSceneSelect : MonoBehaviour
{
	public bool GUIHide = false;
	public bool GUIHide2 = false;
	
	public void LoadSceneMissiles()			{ SceneManager.LoadScene("magic_missiles_demo");		}
	public void LoadSceneCircles()			{ SceneManager.LoadScene("magic_missiles_circles"); 	}
	public void LoadSceneArea01()			{ SceneManager.LoadScene("magic_missiles_area01"); 		}
	public void LoadSceneArea02()			{ SceneManager.LoadScene("magic_missiles_area02"); 		}
	public void LoadSceneArea03()			{ SceneManager.LoadScene("magic_missiles_area03");	 	}


void Update ()
{
 
     if(Input.GetKeyDown(KeyCode.L))
	 {
         GUIHide = !GUIHide;
     
         if (GUIHide)
		 {
             GameObject.Find("CanvasSceneSelect").GetComponent<Canvas> ().enabled = false;
         }
		 
		 else
		 {
             GameObject.Find("CanvasSceneSelect").GetComponent<Canvas> ().enabled = true;
         }
     }
	      if(Input.GetKeyDown(KeyCode.K))
	 {
         GUIHide2 = !GUIHide2;
     
         if (GUIHide2)
		 {
             GameObject.Find("CanvasMissiles").GetComponent<Canvas> ().enabled = false;
         }
		 else
		 {
             GameObject.Find("CanvasMissiles").GetComponent<Canvas> ().enabled = true;
         }
	 }
	 
}

}

}