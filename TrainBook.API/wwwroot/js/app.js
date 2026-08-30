/* ===== TrainBook frontend — เรียก REST API ของ ASP.NET Core ===== */
const API = '';                       // same origin
const TKEY = 'tb_token', UKEY = 'tb_user';

const getToken = () => { try { return localStorage.getItem(TKEY); } catch { return null; } };
const setAuth = (t, u) => { try { t ? localStorage.setItem(TKEY, t) : localStorage.removeItem(TKEY); u ? localStorage.setItem(UKEY, JSON.stringify(u)) : localStorage.removeItem(UKEY); } catch {} };
const getUser = () => { try { return JSON.parse(localStorage.getItem(UKEY) || 'null'); } catch { return null; } };
const store = (k, v) => localStorage.setItem(k, JSON.stringify(v));
const load = (k) => { try { return JSON.parse(localStorage.getItem(k) || 'null'); } catch { return null; } };

async function api(path, { method = 'GET', body } = {}) {
  const h = { 'Content-Type': 'application/json' };
  const t = getToken(); if (t) h['Authorization'] = 'Bearer ' + t;
  const res = await fetch(API + path, { method, headers: h, body: body ? JSON.stringify(body) : undefined });
  let data = null; try { data = await res.json(); } catch {}
  if (!res.ok) throw new Error((data && data.message) || 'เกิดข้อผิดพลาด');
  return data;
}

function toast(msg) {
  let t = document.getElementById('toast');
  if (!t) { t = document.createElement('div'); t.id = 'toast'; t.className = 'toast'; document.body.appendChild(t); }
  t.textContent = msg; t.classList.add('show');
  setTimeout(() => t.classList.remove('show'), 2600);
}
const hhmm = (s) => (s || '').toString().slice(0, 5);
const baht = (n) => Number(n).toLocaleString() + ' บาท';

// อัปเดตเมนู "เข้าสู่ระบบ" ↔ ชื่อผู้ใช้ + ออกจากระบบ
function refreshNav() {
  const link = document.getElementById('authLink');
  if (!link) return;
  const u = getUser();
  // ลิงก์แอดมิน (แสดงเฉพาะ role admin)
  const ul = link.closest('ul');
  let adminLi = document.getElementById('adminNav');
  if (u && u.role === 'admin' && ul && !adminLi) {
    adminLi = document.createElement('li'); adminLi.id = 'adminNav';
    adminLi.innerHTML = '<a href="admin.html" style="color:#ff9800;font-weight:700;">🛠 แอดมิน</a>';
    ul.insertBefore(adminLi, link.parentElement);
  } else if ((!u || u.role !== 'admin') && adminLi) {
    adminLi.remove();
  }
  if (u) {
    link.textContent = 'ออกจากระบบ (' + u.username + ')';
    link.onclick = (e) => { e.preventDefault(); setAuth(null, null); location.href = 'index.html'; };
  } else {
    link.textContent = 'เข้าสู่ระบบ';
    link.setAttribute('href', 'login.html');
  }
}

/* ---------------- per-page ---------------- */
document.addEventListener('DOMContentLoaded', () => {
  refreshNav();
  const page = document.body.dataset.page;
  ({ index: initIndex, search: initSearch, booking: initBooking, ticket: initTicket, history: initHistory,
     login: initLogin, register: initRegister })[page]?.();
});

/* index — โหลดสถานี + ฟอร์มค้นหา */
async function initIndex() {
  try {
    const stations = await api('/api/trains/stations');
    const o = document.getElementById('origin'), d = document.getElementById('destination');
    stations.forEach(s => {
      o.add(new Option(s.stationName, s.stationId));
      d.add(new Option(s.stationName, s.stationId));
    });
    // ตั้งค่าเริ่มต้น: ต้นทาง = กรุงเทพฯ, ปลายทาง = เชียงใหม่ (เส้นทางที่มีเที่ยวรถ)
    const bkk = [...o.options].find(op => op.text.includes('กรุงเทพ'));
    const cnx = [...d.options].find(op => op.text.includes('เชียงใหม่'));
    if (bkk) o.value = bkk.value;
    if (cnx) d.value = cnx.value;
  } catch (e) { toast(e.message); }

  document.getElementById('searchForm').addEventListener('submit', (e) => {
    e.preventDefault();
    const o = document.getElementById('origin'), d = document.getElementById('destination');
    if (o.value === d.value) { toast('ต้นทางและปลายทางต้องไม่เหมือนกัน'); return; }
    store('tb_search', {
      originStationId: +o.value, originName: o.selectedOptions[0].text,
      destinationStationId: +d.value, destName: d.selectedOptions[0].text,
      travelDate: document.getElementById('travelDate').value,
      passengers: +document.getElementById('passengers').value
    });
    location.href = 'search.html';
  });
}

/* search — แสดงผลค้นหา */
async function initSearch() {
  const s = load('tb_search');
  if (!s) { location.href = 'index.html'; return; }
  document.getElementById('routeInfo').textContent = `${s.originName} → ${s.destName} · ${s.travelDate} · ${s.passengers} คน`;
  const wrap = document.getElementById('results');
  let list;
  try {
    list = await api('/api/trains/search', { method: 'POST', body: {
      originStationId: s.originStationId, destinationStationId: s.destinationStationId, travelDate: s.travelDate } });
  } catch (e) { wrap.innerHTML = `<div class="empty">${e.message}</div>`; return; }

  if (!list.length) { wrap.innerHTML = '<div class="empty">ไม่พบเที่ยวรถในเส้นทางนี้</div>'; return; }
  wrap.innerHTML = '';
  list.forEach(t => {
    const card = document.createElement('div');
    card.className = 'card train-card';
    card.innerHTML = `
      <div class="train-info">
        <h3>${t.trainName} (ขบวน ${t.trainNumber})</h3>
        <div style="color:var(--ink-soft);font-size:13px;">${t.originStation} → ${t.destinationStation}</div>
        <div class="train-time">${hhmm(t.departureTime)} <small>→</small> ${hhmm(t.arrivalTime)}</div>
      </div>
      <div class="class-list"></div>`;
    const cl = card.querySelector('.class-list');
    t.classes.forEach(c => {
      const pill = document.createElement('div');
      pill.className = 'class-pill';
      pill.innerHTML = `<div class="cn">${c.className}</div><div class="cp">${c.price.toLocaleString()} ฿</div><div class="cs">ว่าง ${c.availableSeats}</div>`;
      pill.onclick = () => {
        store('tb_selected', {
          scheduleId: t.scheduleId, trainName: t.trainName, trainNumber: t.trainNumber,
          origin: t.originStation, dest: t.destinationStation, dep: hhmm(t.departureTime), arr: hhmm(t.arrivalTime),
          classId: c.classId, className: c.className, price: c.price
        });
        location.href = 'booking.html';
      };
      cl.appendChild(pill);
    });
    wrap.appendChild(card);
  });
}

/* booking — ข้อมูลผู้โดยสาร + ชำระเงิน */
function initBooking() {
  if (!getUser()) { toast('กรุณาเข้าสู่ระบบก่อนจอง'); setTimeout(() => location.href = 'login.html', 800); return; }
  const s = load('tb_search'), sel = load('tb_selected');
  if (!s || !sel) { location.href = 'index.html'; return; }

  document.getElementById('tripInfo').innerHTML =
    `<b>${sel.trainName} (ขบวน ${sel.trainNumber})</b> · ${sel.origin} → ${sel.dest}<br>${s.travelDate} · ${sel.dep}-${sel.arr} · ${sel.className}`;

  const pf = document.getElementById('passengerForms');
  for (let i = 0; i < s.passengers; i++) {
    const b = document.createElement('div');
    b.className = 'passenger-block';
    b.innerHTML = `<h4 style="margin-bottom:10px;color:var(--primary);">ผู้โดยสารคนที่ ${i + 1}</h4>
      <div class="form-row-2">
        <div class="form-group"><label>คำนำหน้า</label><select class="p-title"><option>นาย</option><option>นาง</option><option>นางสาว</option></select></div>
        <div class="form-group"><label>ชื่อ</label><input class="p-first" placeholder="ชื่อจริง"></div>
      </div>
      <div class="form-row-2" style="margin-top:10px;">
        <div class="form-group"><label>นามสกุล</label><input class="p-last" placeholder="นามสกุล"></div>
        <div class="form-group"><label>เลขบัตรประชาชน</label><input class="p-id" placeholder="เลขบัตร"></div>
      </div>`;
    pf.appendChild(b);
  }

  document.querySelectorAll('.pay-option').forEach(o => o.onclick = () => {
    document.querySelectorAll('.pay-option').forEach(x => x.classList.remove('sel'));
    o.classList.add('sel');
  });

  document.getElementById('total').textContent = baht(sel.price * s.passengers);

  document.getElementById('confirmBtn').onclick = async () => {
    const passengers = [...document.querySelectorAll('.passenger-block')].map(b => ({
      title: b.querySelector('.p-title').value,
      firstName: b.querySelector('.p-first').value.trim(),
      lastName: b.querySelector('.p-last').value.trim(),
      idCardNumber: b.querySelector('.p-id').value.trim()
    }));
    if (passengers.some(p => !p.firstName || !p.lastName)) { toast('กรอกชื่อ-นามสกุลผู้โดยสารให้ครบ'); return; }
    const pay = document.querySelector('.pay-option.sel')?.dataset.method || 'card';
    let res;
    try {
      res = await api('/api/bookings', { method: 'POST', body: {
        scheduleId: sel.scheduleId, classId: sel.classId, travelDate: s.travelDate, paymentMethod: pay, passengers } });
    } catch (e) { toast(e.message); return; }
    store('tb_ref', res.bookingReference);
    location.href = 'ticket.html';
  };
}

/* ticket — แสดง e-ticket */
async function initTicket() {
  const ref = load('tb_ref');
  if (!ref) { location.href = 'index.html'; return; }
  let b;
  try { b = await api('/api/bookings/' + ref); } catch (e) { toast(e.message); return; }
  const box = document.getElementById('ticketBox');
  const status = b.bookingStatus === 'CONFIRMED' ? '<span class="badge ok">ชำระแล้ว</span>' : b.bookingStatus;
  box.innerHTML = `
    <div class="ticket">
      <div style="text-align:center;margin-bottom:14px;">
        <div style="font-weight:700;">🚆 การรถไฟแห่งประเทศไทย</div>
        <div style="font-size:12px;color:#cfe0f5;">E-Ticket</div>
      </div>
      <div class="tk-row"><span>รหัสการจอง</span><b>${b.bookingReference}</b></div>
      <div class="tk-row"><span>ขบวน</span><b>${b.schedule.train.trainName} (${b.schedule.train.trainNumber})</b></div>
      <div class="tk-row"><span>เส้นทาง</span><b>${b.schedule.originStation.stationName} → ${b.schedule.destinationStation.stationName}</b></div>
      <div class="tk-row"><span>วันเดินทาง</span><b>${(b.travelDate||'').slice(0,10)}</b></div>
      <div class="tk-row"><span>เวลา</span><b>${hhmm(b.schedule.departureTime)} - ${hhmm(b.schedule.arrivalTime)}</b></div>
      <div class="tk-row"><span>ชั้นโดยสาร</span><b>${b.class.className}</b></div>
      <div class="tk-row"><span>ผู้โดยสาร</span><b>${b.numberOfPassengers} คน</b></div>
      <div class="tk-row"><span>ราคารวม</span><b>฿${Number(b.totalAmount).toLocaleString()}</b></div>
      <div class="tk-qr"><img src="/qrcodes/${b.bookingReference}.svg" alt="QR"></div>
    </div>
    <div style="text-align:center;margin-top:18px;">
      <a class="btn-primary" href="/tickets/${b.bookingReference}.html" target="_blank">เปิดตั๋วแบบเต็ม (PDF/พิมพ์)</a>
      <a class="btn-ghost" href="history.html" style="margin-left:8px;">ประวัติการจอง</a>
    </div>`;
  document.getElementById('statusLine').innerHTML = 'สถานะ: ' + status;
}

/* history */
async function initHistory() {
  const wrap = document.getElementById('historyList');
  if (!getUser()) { wrap.innerHTML = '<div class="empty">กรุณาเข้าสู่ระบบ<br><br><a class="btn-primary" href="login.html">เข้าสู่ระบบ</a></div>'; return; }
  let list;
  try { list = await api('/api/bookings/my-bookings'); } catch (e) { wrap.innerHTML = `<div class="empty">${e.message}</div>`; return; }
  if (!list.length) { wrap.innerHTML = '<div class="empty">ยังไม่มีการจอง<br><br><a class="btn-primary" href="index.html">ค้นหาและจองตั๋ว</a></div>'; return; }
  wrap.innerHTML = '';
  list.forEach(b => {
    const badge = b.bookingStatus === 'CONFIRMED' ? '<span class="badge ok">ชำระแล้ว</span>'
      : b.bookingStatus === 'CANCELLED' ? '<span class="badge cancel">ยกเลิก</span>' : '<span class="badge wait">รอชำระ</span>';
    const c = document.createElement('div');
    c.className = 'card hist-card';
    c.innerHTML = `
      <div>
        <div style="font-size:12px;color:var(--ink-soft);">${b.bookingReference}</div>
        <div style="font-weight:600;font-size:15px;margin:2px 0;">${b.schedule.originStation.stationName} → ${b.schedule.destinationStation.stationName}</div>
        <div style="font-size:12.5px;color:var(--ink-soft);">${(b.travelDate||'').slice(0,10)} · ${hhmm(b.schedule.departureTime)}-${hhmm(b.schedule.arrivalTime)} · ${b.class.className}</div>
      </div>
      <div style="display:flex;align-items:center;gap:12px;">
        <b style="color:var(--primary);">฿${Number(b.totalAmount).toLocaleString()}</b>${badge}
        <a class="btn-ghost" href="/tickets/${b.bookingReference}.html" target="_blank">ดูตั๋ว</a>
      </div>`;
    wrap.appendChild(c);
  });
}

/* login */
function initLogin() {
  document.getElementById('loginForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    const username = document.getElementById('loginUsername').value.trim();
    const password = document.getElementById('loginPassword').value;
    if (!username || !password) { toast('กรอกชื่อผู้ใช้และรหัสผ่าน'); return; }
    let d;
    try { d = await api('/api/auth/login', { method: 'POST', body: { username, password } }); }
    catch (err) { toast(err.message); return; }
    setAuth(d.token, { userId: d.userId, username: d.username, email: d.email, role: d.role });
    toast('เข้าสู่ระบบสำเร็จ');
    setTimeout(() => location.href = 'index.html', 600);
  });
}

/* register */
function initRegister() {
  document.getElementById('registerForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    const pw = document.getElementById('password').value;
    if (pw !== document.getElementById('confirmPassword').value) { toast('รหัสผ่านไม่ตรงกัน'); return; }
    if (pw.length < 6) { toast('รหัสผ่านอย่างน้อย 6 ตัวอักษร'); return; }
    const body = {
      username: document.getElementById('username').value.trim(),
      email: document.getElementById('email').value.trim(),
      password: pw,
      firstName: document.getElementById('firstName').value.trim(),
      lastName: document.getElementById('lastName').value.trim(),
      phoneNumber: document.getElementById('phone').value.trim()
    };
    try { await api('/api/auth/register', { method: 'POST', body }); }
    catch (err) { toast(err.message); return; }
    toast('สมัครสมาชิกสำเร็จ กรุณาเข้าสู่ระบบ');
    setTimeout(() => location.href = 'login.html', 800);
  });
}
