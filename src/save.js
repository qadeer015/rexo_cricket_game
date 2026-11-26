/**
 * localStorage stub. Milestone 1 records a snapshot and can read it back.
 * It does not resume an innings; a later menu can call loadMatch().
 */

const KEY = 'night-nets.m1';

export function saveMatch(data) {
  try {
    localStorage.setItem(KEY, JSON.stringify(data));
    return true;
  } catch {
    return false;
  }
}

export function loadMatch() {
  try {
    const raw = localStorage.getItem(KEY);
    if (!raw) return null;
    const data = JSON.parse(raw);
    if (!data || data.v !== 1) return null;
    return data;
  } catch {
    return null;
  }
}
