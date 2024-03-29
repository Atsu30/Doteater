﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#region AtomClass
public class Atoms
{
    List<Atom> atoms;
    public bool haveH2 { get; private set; }
    public bool haveH2O { get; private set; }

    // 静的メンバ
    // 原子の種類
    public enum Atom
    {
        H,
        H2,
        O,
        H2O
    }

    // 組み合わせ式 [atom1 + atom2 = output]
    public struct Conbination
    {
        public Atom atom1;
        public Atom atom2;
        public Atom output;

        public Conbination(Atom atom1, Atom atom2, Atom output)
        {
            this.atom1 = atom1;
            this.atom2 = atom2;
            this.output = output;
        }
    }

    // 組み合わせ式のリスト
    private static readonly List<Conbination> ConbinationList = new List<Conbination>()
    {
        new Conbination(Atom.H, Atom.H, Atom.H2),
        new Conbination(Atom.H2, Atom.O, Atom.H2O)
    };

    // コンストラクタ
    public Atoms()
    {
        atoms = new List<Atom>();
        haveH2O = false;
    }

    // メソッド

    public string ShowAll()
    {
        string s = "";
        for (int i = 0; i < atoms.Count; i++)
        {
            s += atoms[i].ToString();

            if (i != atoms.Count - 1)
                s += ",";
        }
        return s;
    }

    public void Add(Atom atom)
    {
        if (atom == Atom.H2) this.haveH2 = true;
        if (atom == Atom.H2O) this.haveH2O = true;
        atoms.Add(atom);
        UpdateAtoms();
    }

    private void RemoveAt(int i)
    {
        atoms.RemoveAt(i);
    }

    private void RemoveLast()
    {
        atoms.RemoveAt(atoms.Count - 1);
    }

    private void UpdateAtoms()
    {
        if (atoms.Count < 2) return;

        int cnt = atoms.Count;
        Atom first = atoms[cnt - 1];

        for (int i = cnt - 2; i >= 0; i--)
        {
            foreach (Conbination conb in ConbinationList)
            {
                // 組み合わせリストにあれば、変換
                if (first == conb.atom1 && atoms[i] == conb.atom2 ||
                    atoms[i] == conb.atom1 && first == conb.atom2)
                {
                    this.RemoveLast();
                    this.RemoveAt(i);
                    this.Add(conb.output);
                }
            }
        }
    }
}
# endregion

public class Player : MonoBehaviour
{
    #region propaty
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 360f;
    [SerializeField] private float hp = 1.0f;
    [SerializeField] private Text text = null;
    [SerializeField] private GameObject cam = null;
    [SerializeField] private GameObject waterAttackParticle = null;

    Atoms atoms;

    // public GameObject bulletPrefab;
    // public float speed = 1.0f;
    CharacterController characterController;
    Animator animator;
    GameObject child;
    Collider handCollider;
    #endregion

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        child = transform.Find("unitychan").gameObject;
        atoms = new Atoms();
        handCollider = GameObject.Find("Character1_LeftHand").GetComponent<SphereCollider>();
    }

    void Update()
    {
        //Debug.LogFormat("Frame:{0} , stateIdle: {1}, stateDamaged: {2}, isintransition: {3}", ++frame, animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), animator.GetCurrentAnimatorStateInfo(0).IsName("DamagedWeak"), animator.IsInTransition(0));
        // Jabのコライダーを更新
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Jab")) handCollider.enabled = false;
        // UIテキストの原子リストを更新
        text.text = atoms.ShowAll();

        // Ratate
        Vector3 direction = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        direction = Quaternion.Euler(0, cam.transform.rotation.eulerAngles.y, 0) * direction;
        if (direction.sqrMagnitude > 0.01f)
        {

            Vector3 forward = Vector3.Slerp(
                                  transform.forward,
                                  direction,
                                  rotationSpeed * Time.deltaTime / Vector3.Angle(transform.forward, direction)
                              );
            transform.LookAt(transform.position + forward);
        }

        // 入力はIdle,Runの時のみ受け付ける
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")
            || animator.GetCurrentAnimatorStateInfo(0).IsName("Run"))
        {
            // Move
            characterController.Move(direction * moveSpeed * Time.deltaTime);

            // Attack
            if (Input.GetMouseButtonDown(0))
            {
                // H2Oを持っていれば水で攻撃
                if (atoms.haveH2O)
                {
                    WaterAttack();
                }
                // それ以外はジャブで攻撃
                else
                {
                    Jab();
                }
            }
            else if (Input.GetKeyDown(KeyCode.T))
            {
                animator.SetTrigger("DamagedWeak");
            }
        }

        animator.SetFloat("Speed", characterController.velocity.magnitude);
        characterController.Move(new Vector3(0, Physics.gravity.y * Time.deltaTime * Time.deltaTime, 0)); // 重力を与える


        // アニメーションでこ要素の位置がズレるのを修正
        child.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
        child.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

        // shoot
        // if (Input.GetKeyDown(KeyCode.Space)) Shoot();

        // if (GameObject.FindGameObjectsWithTag("Dot").Length == 0)
        // {
        //     SceneManager.LoadScene("Win");
        // }
    }

    public void Damaged(float attackPoint)
    {
        if (animator.IsInTransition(0) ||
            animator.GetCurrentAnimatorStateInfo(0).IsName("DamagedWeak")) return;

        hp -= attackPoint;

        if (hp <= 0)
        {
            animator.SetBool("Death", true);
        }
        else
        {
            animator.SetTrigger("DamagedWeak");
        }
    }

    void Jab()
    {
        handCollider.enabled = true;
        animator.SetTrigger("Jab");
    }

    void WaterAttack()
    {
        animator.SetTrigger("WaterAttack");
        Vector3 pos = transform.position + transform.rotation * new Vector3(0.0f, 1.0f, 1.0f);
        Quaternion rot = transform.rotation * Quaternion.Euler(-90, 0, 0);
        GameObject obj = Instantiate(waterAttackParticle, pos, rot);

        float duration = obj.GetComponent<ParticleSystem>().main.duration;
        Destroy(obj, duration);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Dot")
        {
            Dot dot = other.GetComponent<Dot>();
            dot.getDot();
            atoms.Add(dot.type);
        }
        else if (other.tag == "Enemy")
        {
            //SceneManager.LoadScene("Lose");
        }
    }

    // void Shoot()
    // {
    //     GameObject bullet = Instantiate(bulletPrefab) as GameObject;
    //     Vector3 force = this.transform.forward * speed;
    //     bullet.GetComponent<Rigidbody>().AddForce(force, ForceMode.Impulse);
    //     bullet.transform.position = this.transform.position + new Vector3(0.0f, 0.5f, 0.0f);
    // }
}
