using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class GestionMenu : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelInicio;
    public GameObject panelNombre;
    public GameObject panelLobby;

    [Header("Elementos de Nombre")]
    public TMP_InputField inputNombre;
    public Button botonAceptar;

    private bool juegoIniciado = false;

    [Header("Efecto Parpadeo")]
    public TextMeshProUGUI textoPulsaJugar;
    public float velocidadParpadeo = 2f;

    void Start()
    {
        panelInicio.SetActive(true);
        panelNombre.SetActive(false);
        panelLobby.SetActive(false);

        botonAceptar.interactable = false;

        inputNombre.onValueChanged.AddListener(delegate { ValidarNombre(); });

        StartCoroutine(EfectoParpadeoSuave());
    }

    void Update()
    {
        if (!juegoIniciado && panelInicio.activeSelf)
        {
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                IniciarJuego();
            }
        }
    }

    public void IniciarJuego()
    {
        juegoIniciado = true;
        panelInicio.SetActive(false);
        panelNombre.SetActive(true);
    }

    public void ValidarNombre()
    {
        Debug.Log("Escribiendo: " + inputNombre.text);

        botonAceptar.interactable = (inputNombre.text.Trim().Length > 0);
    }

    public void ConfirmarNombre()
    {
        panelNombre.SetActive(false);
        panelLobby.SetActive(true);
    }

    IEnumerator EfectoParpadeoSuave()
    {
        while (panelInicio.activeSelf)
        {
            float tiempo = 0;
            while (tiempo < 1)
            {
                textoPulsaJugar.alpha = Mathf.PingPong(Time.time * velocidadParpadeo, 1);
                yield return null;
            }
        }
    }
}
