/* ===== Admin dashboard ===== */
const TKEY = 'tb_token', UKEY = 'tb_user';
const getToken = () => { try { return localStorage.getItem(TKEY); } catch { return null; } };
const getUser = () => { try { return JSON.parse(localStorage.getItem(UKEY) || 'null'); } catch { return null; } };

async function api(path, { method = 'GET', body } = {}) {
  const h = { 'Content-Type': 'application/json' };
  const t = getToken(); if (t) h['Authorization'] = 'Bearer ' + t;
  const res = await fetch(path, { method, headers: h, body: body ? JSON.stringify(body) : undefined });
  let d = null; try { d = await res.json(); } catch {}
  if (res.status === 401 || res.status === 403) { alert('หน้านี้สำหรับผู้ดูแลระบบเท่านั้น'); location.href = 'index.html'; throw new Error('forbidden'); }
  if (!res.ok) throw new Error((d && d.message) || 'เกิดข้อผิดพลาด');
  return d;
}
function toast(msg) {
  let t = document.getElementById('toast');
  if (!t) { t = document.createElement('div'); t.id = 'toast'; t.className = 'toast'; document.body.appendChild(t); }
  t.textContent = msg; t.classList.add('show'); setTimeout(() => t.classList.remove('show'), 2500);
}
const baht = n => Number(n).toLocaleString();
const hhmm = s => (s || '').toString().slice(0, 5);
let META = { trains: [], stations: [], classes: [] };

/* guard + init */
document.addEventListener('DOMContentLoaded', async () => {
  const u = getUser();
  if (!u || u.role !== 'admin') { alert('หน้านี้สำหรับผู้ดูแลระบบเท่านั้น'); location.href = 'index.html'; return; }
  document.getElementById('adminName').textContent = '👤 ' + u.username;
  document.getElementById('logoutBtn').onclick = (e) => { e.preventDefault(); localStorage.removeItem(TKEY); localStorage.removeItem(UKEY); location.href = 'index.html'; };
  try {
    META = await api('/api/admin/meta');
    loadSummary(); loadSchedules(); loadBookings(); loadUsers();
  } catch (e) { /* redirected */ }
});

function switchTab(tab, btn) {
  document.querySelectorAll('.tabbar button').forEach(b => b.classList.remove('active'));
  btn.classList.add('active');
  ['schedules', 'bookings', 'users'].forEach(t => document.getElementById('tab-' + t).style.display = t === tab ? 'block' : 'none');
}

async function loadSummary() {
  const s = await api('/api/admin/summary');
  stUsers.textContent = s.users; stSchedules.textContent = s.schedules;
  stBookings.textContent = s.bookings; stRevenue.textContent = baht(s.revenue);
}

async function loadSchedules() {
  const list = await api('/api/admin/schedules');
  const tb = document.getElementById('scheduleRows'); tb.innerHTML = '';
  list.forEach(s => {
    const prices = s.prices.map(p => `${p.className} (${baht(p.priceAmount)}฿·${p.availableSeats})`).join('<br>');
    const tr = document.createElement('tr');
    tr.innerHTML = `
      <td><b>${s.trainNumber}</b><br><span style="font-size:12px;color:var(--ink-soft);">${s.trainName}</span></td>
      <td>${s.origin} → ${s.dest}</td>
      <td>${hhmm(s.departureTime)} - ${hhmm(s.arrivalTime)}</td>
      <td style="font-size:12px;">${prices || '-'}</td>
      <td>${s.isActive ? '<span class="badge ok">เปิดขาย</span>' : '<span class="badge cancel">ปิด</span>'}</td>
      <td><div style="display:flex;gap:6px;flex-wrap:wrap;">
        <button class="btn-ghost mini" onclick="toggleActive(${s.scheduleId})">${s.isActive ? 'ปิดขาย' : 'เปิดขาย'}</button>
        <button class="btn-ghost mini" style="color:var(--danger);border-color:#f3d0d0;" onclick="deleteSchedule(${s.scheduleId})">ลบ</button>
      </div></td>`;
    tb.appendChild(tr);
  });
}

async function toggleActive(id) {
  try { await api('/api/admin/schedules/' + id + '/active', { method: 'PATCH' }); } catch (e) { toast(e.message); return; }
  loadSchedules();
}
async function deleteSchedule(id) {
  if (!confirm('ลบเที่ยวรถนี้? การจองที่เกี่ยวข้องจะถูกลบด้วย')) return;
  try { await api('/api/admin/schedules/' + id, { method: 'DELETE' }); } catch (e) { toast(e.message); return; }
  toast('ลบเที่ยวรถแล้ว'); loadSchedules(); loadSummary();
}

async function loadBookings() {
  const list = await api('/api/admin/bookings');
  const tb = document.getElementById('bookingRows'); tb.innerHTML = '';
  if (!list.length) { tb.innerHTML = '<tr><td colspan="7" style="text-align:center;color:var(--ink-soft);">ยังไม่มีการจอง</td></tr>'; return; }
  list.forEach(b => {
    const badge = b.bookingStatus === 'CONFIRMED' ? '<span class="badge ok">ชำระแล้ว</span>'
      : b.bookingStatus === 'CANCELLED' ? '<span class="badge cancel">ยกเลิก</span>' : '<span class="badge wait">รอชำระ</span>';
    const action = b.bookingStatus === 'CANCELLED'
      ? `<button class="btn-ghost mini" onclick="setStatus(${b.bookingId},'CONFIRMED')">คืนสถานะ</button>`
      : `<button class="btn-ghost mini" style="color:var(--danger);border-color:#f3d0d0;" onclick="setStatus(${b.bookingId},'CANCELLED')">ยกเลิก</button>`;
    const tr = document.createElement('tr');
    tr.innerHTML = `
      <td style="font-size:12px;">${b.bookingReference}</td>
      <td>${b.userName}<br><span style="font-size:11px;color:var(--ink-soft);">${b.userEmail}</span></td>
      <td>${b.origin} → ${b.dest}<br><span style="font-size:11px;color:var(--ink-soft);">ขบวน ${b.trainNumber}</span></td>
      <td>${(b.travelDate||'').slice(0,10)}</td>
      <td><b>${baht(b.totalAmount)}</b></td>
      <td>${badge}</td>
      <td>${action}</td>`;
    tb.appendChild(tr);
  });
}
async function setStatus(id, status) {
  try { await api('/api/admin/bookings/' + id + '/status', { method: 'PATCH', body: { status } }); } catch (e) { toast(e.message); return; }
  toast('อัปเดตสถานะแล้ว'); loadBookings(); loadSummary();
}

async function loadUsers() {
  const list = await api('/api/admin/users');
  const tb = document.getElementById('userRows'); tb.innerHTML = '';
  list.forEach(u => {
    const role = u.role === 'admin' ? '<span class="badge wait">แอดมิน</span>' : '<span class="badge ok">สมาชิก</span>';
    const tr = document.createElement('tr');
    tr.innerHTML = `<td>${u.userId}</td><td>${u.username}</td><td>${u.firstName} ${u.lastName}</td><td>${u.email}</td><td>${role}</td>`;
    tb.appendChild(tr);
  });
}

/* ---- add schedule form ---- */
function fillSelect(sel, items, valKey, textFn) {
  sel.innerHTML = '';
  items.forEach(it => sel.add(new Option(textFn(it), it[valKey])));
}
function addPriceRow() {
  const wrap = document.getElementById('priceRows');
  const row = document.createElement('div'); row.className = 'price-row';
  const opts = META.classes.map(c => `<option value="${c.classId}">${c.className}</option>`).join('');
  row.innerHTML = `<select class="pc-class">${opts}</select>
    <input class="pc-price" type="number" placeholder="850">
    <input class="pc-seats" type="number" placeholder="40" value="40">
    <button class="del-x" onclick="this.parentElement.remove()">×</button>`;
  wrap.appendChild(row);
}
function openScheduleForm() {
  fillSelect(document.getElementById('fTrain'), META.trains, 'trainId', t => `ขบวน ${t.trainNumber} (${t.trainName})`);
  fillSelect(document.getElementById('fOrigin'), META.stations, 'stationId', s => s.stationName);
  fillSelect(document.getElementById('fDest'), META.stations, 'stationId', s => s.stationName);
  if (document.getElementById('fDest').options.length > 1) document.getElementById('fDest').selectedIndex = 1;
  document.getElementById('priceRows').innerHTML = ''; addPriceRow();
  ['fDep', 'fArr', 'fDur'].forEach(id => document.getElementById(id).value = '');
  document.getElementById('overlay').classList.add('show');
}
function closeScheduleForm() { document.getElementById('overlay').classList.remove('show'); }

async function saveSchedule() {
  const prices = [...document.querySelectorAll('#priceRows .price-row')].map(r => ({
    classId: +r.querySelector('.pc-class').value,
    price: +r.querySelector('.pc-price').value,
    availableSeats: +r.querySelector('.pc-seats').value || 40
  })).filter(p => p.price > 0);
  const body = {
    trainId: +document.getElementById('fTrain').value,
    originStationId: +document.getElementById('fOrigin').value,
    destinationStationId: +document.getElementById('fDest').value,
    departureTime: document.getElementById('fDep').value.trim(),
    arrivalTime: document.getElementById('fArr').value.trim(),
    durationMinutes: +document.getElementById('fDur').value || 0,
    prices
  };
  if (!body.departureTime || !body.arrivalTime) { toast('กรอกเวลาออก-ถึง'); return; }
  if (!prices.length) { toast('เพิ่มชั้นโดยสารอย่างน้อย 1 ชั้น'); return; }
  if (body.originStationId === body.destinationStationId) { toast('ต้นทาง/ปลายทางต้องไม่เหมือนกัน'); return; }
  try { await api('/api/admin/schedules', { method: 'POST', body }); } catch (e) { toast(e.message); return; }
  toast('เพิ่มเที่ยวรถแล้ว'); closeScheduleForm(); loadSchedules(); loadSummary();
}
