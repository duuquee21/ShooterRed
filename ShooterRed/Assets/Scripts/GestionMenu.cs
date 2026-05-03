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

        // Esto obliga al script a escuchar al InputField automáticamente
        inputNombre.onValueChanged.AddListener(delegate { ValidarNombre(); });

        StartCoroutine(EfectoParpadeoSuave());
    }

    void Update()
    {
        // Si el panel de inicio está activo y el jugador pulsa la pantalla o el ratón
        if (!juegoIniciado && panelInicio.activeSelf)
        {
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                IniciarJuego();
            }
        }
    }

    // Se ejecuta al hacer clic en la pantalla de inicio
    public void IniciarJuego()
    {
        juegoIniciado = true;
        panelInicio.SetActive(false); // Cierra la imagen de inicio
        panelNombre.SetActive(true);  // Abre el mini panel de nombre
    }

    // Paso 2: Validar si el nombre no está vacío
    public void ValidarNombre()
    {
        // Debug para ver en la consola si el script recibe el texto
        Debug.Log("Escribiendo: " + inputNombre.text);

        // Solo se activa si la longitud es mayor a 0 y no son solo espacios
        botonAceptar.interactable = (inputNombre.text.Trim().Length > 0);
    }

    // Paso 3: Al dar clic en Aceptar
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
                // Oscila el alfa entre 0 y 1 usando un Seno para que sea fluido
                textoPulsaJugar.alpha = Mathf.PingPong(Time.time * velocidadParpadeo, 1);
                yield return null;
            }
        }
    }
}
