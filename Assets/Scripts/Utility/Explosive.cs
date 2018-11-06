/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 13 February 2018
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosive : MonoBehaviour
{
    /// <summary>
    /// Only host can send this message. Explosives controlled by host. 
    /// When it's exploded, host (master client I mean) send the Exploded info to other clients.
    /// </summary>
    public class Exploded : SNet_Network.SNetMessage
    {
        public Exploded (ulong _identity)
        {
            i = _identity;
        }
    }

    /// <summary>
    /// This will be instanced when exploded.
    /// </summary>
    public Transform explosionParticle;
    /// <summary>
    /// Mostly used for mines/traps. For example If it's 3, it will explode when touch something but after 3 seconds. 
    /// </summary>
    public float activateIn = 0;
    /// <summary>
    /// Explode when touch anything
    /// </summary>
    public bool explodeOnHitAnything;
    /// <summary>
    /// If this is 0, it will never explode in time.
    /// </summary>
    public float explodeInTime;
    /// <summary>
    /// Explosion radius
    /// </summary>
    public float radius;
    /// <summary>
    /// Explosion Damage
    /// </summary>
    public ushort damage;

    float startTime;
    private void Start()
    {
        tag = "Explosive";

        startTime = Time.time;

        if (explodeInTime > 0)
            MonoBehaviourExtensions.Invoke(this, Explode_Request, explodeInTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!SNet_Network.instance.isHost())
            return;

        if (activateIn != 0 && Time.time < startTime + activateIn)
            return; // Not activated yet

        if (explodeOnHitAnything)
        {
            Explode_Request();
        }
    }

    public void Explode_Request()
    {
        if (!SNet_Network.instance.isHost())
            return;
        /*
        /* SEND EXPLODED MESSAGE
        /* */
            SNet_Network.instance.Send_Message(new Exploded(GetComponent<SNet_Identity>().identity));
        /*
         * */
    }

    bool exploded = false;
    public void Explode()
    {
        if (exploded)
        {
            return;
        }

        exploded = true;

        /*
         * EXPLOSIVE EFFECT
         * */
        Destroy(Instantiate(explosionParticle, transform.position, explosionParticle.rotation).gameObject, 5);
        /*
         * */

        if (!SNet_Network.instance.isHost())
            return;

        // HITTING

        Collider[] colliders = Physics.OverlapSphere(transform.position, radius, ~SNet_Controller.hitMask);

        List<SNet_Identity> alreadyHit = new List<SNet_Identity>();
        foreach (Collider c in colliders)
        {
            if (c.gameObject == gameObject)
                continue;

            float distance = Vector3.Distance(transform.position, c.transform.position);
            int tDamage = Mathf.RoundToInt(damage / (1 + Mathf.Pow(distance, 2) / 10));

            if (c.gameObject.layer == 9)
            {
                // Player
                SNet_Identity id = c.transform.root.GetComponent<SNet_Identity>();

                if (!alreadyHit.Contains(id) && !id.controller.isDead)
                { // If its not dead.
                    alreadyHit.Add(id);

                    SNet_Controller.UserHit hit = new SNet_Controller.UserHit(id.identity);
                    hit.v = tDamage;
                    hit.h = transform.position;
                    SNet_Network.instance.Send_Message(hit);

                    // Ragdoll bone push
                    SNet_Network.instance.Send_Message(new RagdollHelper.Impact(id.identity, c.gameObject.name, c.transform.position - transform.position, tDamage / 150f));
                }
            }
            else
            {
                SNet_Identity id = c.GetComponent<SNet_Identity>();
                if (id != null)
                {
                    if (id.rbody != null)
                    {
                        id.rbody.rBody.AddExplosionForce((radius - distance) * damage * id.rbody.rBody.mass / 4, transform.position, radius);
                        id.rbody.nextUpdate = 0; // Update now.
                    }

                    if (id.vehicle != null)
                    {
                        /*
                         * VEHICLE HEALTH IS HOST CONTROLLED
                         * */
                        if (SNet_Network.instance.isHost())
                        {
                            if (!alreadyHit.Contains(id))
                            {
                                alreadyHit.Add(id);
                                id.vehicle.health = new SNet_Vehicle.VH(id.identity, Mathf.RoundToInt (id.vehicle.health.v - tDamage/id.vehicle.armor));
                                SNet_Network.instance.Send_Message(id.vehicle.health);
                            }
                        }
                        /*
                         * */
                    }
                }


                if (c.gameObject.CompareTag("Explosive"))
                {
                    c.gameObject.GetComponent<Explosive>().Explode_Request();
                }
            }
        }

        Destroy(gameObject); // Destroy after explode.
    }
}
