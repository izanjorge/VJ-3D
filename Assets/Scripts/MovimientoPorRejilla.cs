using UnityEngine;

public class MovimientoPorRejilla : MonoBehaviour
{
    public float velocidadMovimiento = 5f;
    private Vector3 posicionDestino;
    private bool estaMovientose = false;

    void Start()
    {
        // Inicializamos el destino en la posición actual
        posicionDestino = transform.position;
    }

    void Update()
    {
        if (estaMovientose)
        {
            // Movemos suavemente el personaje hacia la casilla destino
            transform.position = Vector3.MoveTowards(transform.position, posicionDestino, velocidadMovimiento * Time.deltaTime);

            // Si ya casi hemos llegado, fijamos la posición y permitimos otro movimiento
            if (Vector3.Distance(transform.position, posicionDestino) < 0.01f)
            {
                transform.position = posicionDestino;
                estaMovientose = false;
            }
        }
        else
        {
            // Capturamos el movimiento solo si no se está moviendo ya
            float horizontal = Input.GetAxisRaw("Horizontal"); // Flechas Izquierda/Derecha o A/D
            float vertical = Input.GetAxisRaw("Vertical");     // Flechas Arriba/Abajo o W/S

            if (horizontal != 0)
            {
                DefinirDestino(new Vector3(horizontal, 0, 0));
            }
            else if (vertical != 0)
            {
                DefinirDestino(new Vector3(0, 0, vertical));
            }
        }
    }

    private void DefinirDestino(Vector3 direccion)
    {
        posicionDestino = transform.position + direccion;
        estaMovientose = true;
        
        // Opcional: Rotar el personaje hacia la dirección del movimiento
        transform.forward = direccion;
    }
}