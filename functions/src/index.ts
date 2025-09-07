// Firebase Functions v2 (TypeScript)
// ORT: админ создаёт пользователей, меняет роль и удаляет поля профиля.
// Роли: student | teacher | admin
// Регион развёртывания: us-central1

import { onCall, CallableRequest, HttpsError } from "firebase-functions/v2/https";
import { initializeApp } from "firebase-admin/app";
import { getAuth } from "firebase-admin/auth";
import {
  getFirestore,
  FieldValue,
  Transaction,
  DocumentSnapshot,
} from "firebase-admin/firestore";

initializeApp();
const db = getFirestore();
const auth = getAuth();

const ALLOWED_ROLES = ["student", "teacher", "admin"] as const;
type AllowedRole = (typeof ALLOWED_ROLES)[number];

// ---------- helpers ----------
function digitsOnly(s?: string): string {
  return (s ?? "").replace(/\D+/g, "");
}

function normalizeRole(raw: any): AllowedRole {
  const role = String(raw ?? "student").toLowerCase();
  if (!ALLOWED_ROLES.includes(role as AllowedRole)) {
    throw new HttpsError("invalid-argument", "invalid role");
  }
  return role as AllowedRole;
}

async function callerIsAdmin(req: CallableRequest<any>): Promise<boolean> {
  if (!req.auth?.uid) return false;
  const snap: DocumentSnapshot = await db.doc(`users/${req.auth.uid}`).get();
  const role = (snap.get("role") || "").toString().toLowerCase();
  return role === "admin";
}

async function assertAdmin(req: CallableRequest<any>) {
  if (!(await callerIsAdmin(req))) {
    throw new HttpsError("permission-denied", "Admins only");
  }
}

// ---------- createUserAsAdmin ----------
export const createUserAsAdmin = onCall({ region: "us-central1" }, async (req) => {
  await assertAdmin(req);

  const data: any = req.data || {};
  const username = String(data.username ?? "").trim().toLowerCase(); // email = username@ort.app
  const realName = String(data.realName ?? "").trim();
  const password = String(data.password ?? "");
  const role = normalizeRole(data.role);

  // фиксированные дополнительные поля (как сейчас в приложении)
  let region = String(data.region ?? "").trim();
  let district = String(data.district ?? "").trim();
  let school = String(data.school ?? "").trim();
  let phone = digitsOnly(data.phone);
  let whatsapp = digitsOnly(data.whatsapp) || phone;

  // универсальные дополнительные поля: кладём всё, что пришло в data.extras{ key: string }
  const rawExtras = data.extras && typeof data.extras === "object" ? data.extras : {};
  const extras: Record<string, string> = {};
  for (const [k, v] of Object.entries(rawExtras)) {
    if (typeof v === "string") {
      const key = k.trim().toLowerCase();
      const val = v.trim();
      if (key && val && key.length <= 40 && val.length <= 200) {
        extras[key] = val;
      }
    }
  }

  if (!username) throw new HttpsError("invalid-argument", "username required");
  if (!realName) throw new HttpsError("invalid-argument", "realName required");
  if (password.length < 6) throw new HttpsError("invalid-argument", "weak password");

  const email = `${username}@ort.app`;
  const unameRef = db.collection("usernames").doc(username);

  // резервируем ник транзакцией
  await db.runTransaction(async (tx: Transaction) => {
    const snap = await tx.get(unameRef);
    if (snap.exists) throw new HttpsError("already-exists", "username taken");
    tx.set(unameRef, { reservedAt: FieldValue.serverTimestamp() });
  });

  try {
    // создаём пользователя в Auth
    const userRecord = await auth.createUser({
      email,
      password,
      displayName: realName,
      disabled: false,
    });
    const uid = userRecord.uid;

    // на будущее: клейм роли
    await auth.setCustomUserClaims(uid, { role });

    // пишем профиль в Firestore
    await db.collection("users").doc(uid).set(
      {
        username,
        realName,
        role,
        createdAt: FieldValue.serverTimestamp(),
        region,
        district,
        school,
        phone,
        whatsapp,
        extras, // <- здесь лежат любые новые пункты
      },
      { merge: true }
    );

    // link username -> uid
    await unameRef.set({ uid }, { merge: true });

    // Unity ждёт uid
    return { uid };
  } catch (e: any) {
    // снимаем резерв ника
    await unameRef.delete().catch(() => {});
    if (e instanceof HttpsError) throw e;
    if (e?.code === "auth/email-already-exists") {
      throw new HttpsError("already-exists", "username taken");
    }
    throw new HttpsError("internal", e?.message || "create failed");
  }
});

// ---------- setUserRole (смена роли существующему пользователю) ----------
export const setUserRole = onCall({ region: "us-central1" }, async (req) => {
  await assertAdmin(req);
  const data: any = req.data || {};
  const uid = String(data.uid ?? "");
  const role = normalizeRole(data.role);
  if (!uid) throw new HttpsError("invalid-argument", "uid required");

  await auth.setCustomUserClaims(uid, { role });
  await db.collection("users").doc(uid).set({ role }, { merge: true });
  return { ok: true };
});

// ---------- removeUserFields (удаление пунктов из профиля) ----------
/**
 * Удаляет поля у пользователя:
 *  - fields: массив имён top-level полей (например "region","school","phone")
 *  - extras: массив ключей внутри extras (например "social","instagram")
 */
export const removeUserFields = onCall({ region: "us-central1" }, async (req) => {
  await assertAdmin(req);

  const { uid, fields = [], extras = [] } = req.data || {};
  if (!uid) throw new HttpsError("invalid-argument", "uid required");
  if (!Array.isArray(fields) || !Array.isArray(extras)) {
    throw new HttpsError("invalid-argument", "fields/extras must be arrays");
  }

  const update: Record<string, any> = {};
  for (const f of fields) {
    if (typeof f === "string" && f.trim()) update[f.trim()] = FieldValue.delete();
  }
  for (const k of extras) {
    if (typeof k === "string" && k.trim()) update[`extras.${k.trim()}`] = FieldValue.delete();
  }

  if (Object.keys(update).length === 0) return { ok: true, skipped: true };

  await db.doc(`users/${uid}`).set(update, { merge: true });
  return { ok: true };
});
