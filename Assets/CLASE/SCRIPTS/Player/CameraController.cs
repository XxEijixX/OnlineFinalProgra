using System;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Fusion.Addons.KCC;

public class CameraController : NetworkBehaviour
{
    [Networked] public float camY { get; set; } // Añadido para sincronización

    [Header("Camera Settings")]
    [SerializeField]
    private Transform player;

    [SerializeField] private float mouseSensitivity = 1;

    [FormerlySerializedAs("smooth")]
    [SerializeField]
    private float smoothnes;

    [SerializeField] private float maxAngleY = 80;
    [SerializeField] private float minAngleY = -80;

    private Vector2 camVelociy;
    private Vector2 smoothVelocity;

    [Header("Blob Movement")]
    [SerializeField] private float walkingSpeed = 1f;

    [SerializeField, Range(0, 0.1f)] private float walkingAmplitude = 0.015f; // Que tanto se mueve hacia los lados al caminar
    [SerializeField, Range(0, 0.1f)] private float runningAmplitude = 0.015f; // Que tanto se mueve hacia los lados al correr
    [SerializeField, Range(0, 15)] private float walkingFrequency = 10.0f; // La frecuencia con la que se mueve al caminar
    [SerializeField, Range(10, 20)] private float runningFrequency = 18f; // La frecuencia con la que se mueve al correr
    [SerializeField] private float resetPosSpeed = 3.0f; // Cuando dejas de moverte que regrese al centro
    [SerializeField] private float toggleSpeed = 3.0f; // 

    private Vector3 startPos; // Posicion inicial de la cabeza , el centro

    [SerializeField] private bool moveHead;

    private Vector2 head;

    private InputManager inputManager;
    private KCC kcc; // Referencia al KCC

    private void Awake()
    {
        startPos = transform.localPosition;
    }

    private void Start()
    {
        inputManager = InputManager.Instance;
        if (player == null)
        {
            player = FindObjectOfType<MovementController>().transform;
        }

        // Obtener referencia al KCC del jugador
        if (player != null)
        {
            kcc = player.GetComponent<KCC>();
        }

        Cursor.lockState = CursorLockMode.None;
        //Cursor.visible = false;
    }

    public override void Spawned() // Este metodo se manda a llamar despues de hacer spawn
    {
        if (!HasInputAuthority) // Si no es el jugador al que le di autoridad
        {
            GetComponent<Camera>().enabled = false;
            GetComponent<AudioListener>().enabled = false;
        }
    }

    /// <summary>
    /// El Render() manda a photon lo que esta adentro x veces por segundo segun tu framerate
    /// 
    /// Lo usamos para cambios meramente visuales 
    /// </summary>
    public override void Render()
    {
        if (!HasInputAuthority && !HasStateAuthority)
        {
            transform.localRotation = Quaternion.AngleAxis(-camY, Vector3.right);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasInputAuthority)
        {
            if (GetInput(out NetworkInputData input)) // Aqui, yo debo cersiorarme de estar recibiendo el input del servidor. Me consigue el input que me manda el servidor
            {
                RotateCamera(input);
            }
        }
    }

    private void RotateCamera(NetworkInputData input)
    {
        Vector2 rawFrameVelocity = Vector2.Scale(input.look, Vector2.one * mouseSensitivity);
        smoothVelocity = Vector2.Lerp(smoothVelocity, rawFrameVelocity, 1 / smoothnes); // Te mueve desde donde tengas el mouse, a la nueva posicion del mouse
        camVelociy += smoothVelocity;
        camVelociy.y = Mathf.Clamp(camVelociy.y, minAngleY, maxAngleY); // Limita la rotacion de la camara en Y. En base el movimiento del mouse.

        // Sincronizar la rotación Y para todos los jugadores
        camY = camVelociy.y;

        // Solo el jugador local aplica su propia rotación completa
        if (HasInputAuthority)
        {
            transform.localRotation = Quaternion.AngleAxis(-camVelociy.y, Vector3.right); // Rota la camara hacia arriba y abajo. La rotacion esta en X. 

            // Rotación del jugador usando KCC
            if (kcc != null)
            {
                // Usar el KCC para rotar el personaje
                kcc.SetLookRotation(Quaternion.AngleAxis(camVelociy.x, Vector3.up));
            }
            else
            {
                // Fallback por si no encuentra el KCC
                player.rotation = Quaternion.AngleAxis(camVelociy.x, Vector3.up);
            }
        }
    }

    private void BlobMove()
    {
        if (!inputManager.IsMoveInputPressed()) // Si no presiono ningun input
        {
            return; // termina el metodo
        }

        if (inputManager.IsMoveInputPressed()) // Pregunto si me estoy moviendo
        {
            if (inputManager.IsMovingBackwards() || inputManager.IsMovingOnXAxis()) // Me estoy moviendo hacia atras o hacia los lados?
            {
                transform.localPosition += FootStepMotion();
            }
            else //  Entonces me muevo hacia adelante
            {
                if (inputManager.WasRunInputPressed()) // Estoy corriendo?
                {
                    transform.localPosition += RunningFootStepMotion();
                }
                else // Estoy caminando
                {
                    transform.localPosition += FootStepMotion();
                }
            }
        }

        if (inputManager.IsMoveInputPressed())
        {
            transform.localPosition += inputManager.IsMovingBackwards() || inputManager.IsMovingOnXAxis() ? FootStepMotion() : inputManager.WasRunInputPressed() ? RunningFootStepMotion() : FootStepMotion();
        }
    }

    private void ResetPosition()
    {
        if (transform.localPosition == startPos) return; // Si la camara ya esta en la pos inicial, no hace nada
        transform.localPosition = Vector3.Lerp(transform.localPosition, startPos, resetPosSpeed * Time.deltaTime);
    }

    private Vector3 FootStepMotion()
    {
        Vector3 pos = Vector3.zero;
        pos.y = Mathf.Sin(Time.time * walkingFrequency) * walkingAmplitude * walkingSpeed;
        pos.x = Mathf.Cos(Time.time * walkingFrequency / 2) * walkingAmplitude * 2 * walkingSpeed;
        return pos;
    }

    private Vector3 RunningFootStepMotion()
    {
        Vector3 pos = Vector3.zero;
        pos.y = Mathf.Sin(Time.time * runningFrequency) * runningAmplitude * walkingSpeed;
        pos.x = Mathf.Cos(Time.time * runningFrequency / 2) * runningAmplitude * 2 * walkingSpeed;
        return pos;
    }
}