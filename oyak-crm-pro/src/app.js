import { Store } from './store.js';

const Icons = {
    dashboard: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="3" width="7" height="7"></rect><rect x="14" y="3" width="7" height="7"></rect><rect x="14" y="14" width="7" height="7"></rect><rect x="3" y="14" width="7" height="7"></rect></svg>',
    users: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path><circle cx="9" cy="7" r="4"></circle></svg>',
    calendar: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect><line x1="16" y1="2" x2="16" y2="6"></line><line x1="8" y1="2" x2="8" y2="6"></line><line x1="3" y1="10" x2="21" y2="10"></line></svg>',
    mail: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"></path><polyline points="22,6 12,13 2,6"></polyline></svg>',
    plus: '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>',
    check: '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"></polyline></svg>'
};

class App {
    constructor() {
        this.appRef = document.getElementById('app');
        this.currentView = 'dashboard';
        this.selectedId = null;
        this.tempDate = null;
        
        Store.init();
        Store.subscribe(() => this.render());
        this.render();
    }

    setView(view, id = null) { 
        this.currentView = view; 
        this.selectedId = id;
        this.render(); 
        window.scrollTo(0,0);
    }

    render() {
        if (!this.appRef) return;
        this.appRef.innerHTML = `
            <aside class="sidebar glass">
                <div class="logo" onclick="window.app.setView('dashboard')">
                    <img src="oypa_logo.png" alt="OYPA Logo" onerror="this.src='https://placehold.co/40x40/D4AF37/002B5B?text=O'">
                    <h1>OYPA<span>CRM</span></h1>
                </div>
                <nav class="nav-menu">
                    <div class="nav-item ${this.currentView === 'dashboard' ? 'active' : ''}" onclick="window.app.setView('dashboard')">${Icons.dashboard}<span>Genel Bakış</span></div>
                    <div class="nav-item ${this.currentView === 'leads' ? 'active' : ''}" onclick="window.app.setView('leads')">${Icons.users}<span>Leads & Opportunities</span></div>
                    <div class="nav-item ${this.currentView === 'customers' ? 'active' : ''}" onclick="window.app.setView('customers')">${Icons.users}<span>Müşterilerimiz</span></div>
                    <div class="nav-item ${this.currentView === 'calendar' ? 'active' : ''}" onclick="window.app.setView('calendar')">${Icons.calendar}<span>Ziyaret Takvimi</span></div>
                    <div class="nav-item ${this.currentView === 'drafts' ? 'active' : ''}" onclick="window.app.setView('drafts')">${Icons.mail}<span>Mail Taslakları</span></div>
                </nav>
                <div class="side-module glass" style="margin-top:auto; padding:15px; border-radius:12px;">
                    <h4 style="font-size:0.7rem; margin-bottom:10px; color:var(--accent-gold); text-transform:uppercase;">Yaklaşanlar</h4>
                    <div id="upcoming-list">${this.renderUpcomingShort()}</div>
                </div>
            </aside>
            <main class="main-content">
                <header class="main-header glass">
                    <div style="display:flex; align-items:center; gap:15px;">
                        ${['detail', 'profile'].includes(this.currentView) ? `<button class="btn btn-ghost btn-sm" onclick="window.app.setView('dashboard')">← Geri</button>` : ''}
                        <h2 style="font-size:1.1rem; color:var(--text-muted);">${this.getViewTitle()}</h2>
                    </div>
                    <div class="header-right">
                        <div class="notification-bell" onclick="window.app.showNotifications()">🔔 <span class="badge-count">${Store.state.notifications.length}</span></div>
                        <div class="user-profile" onclick="window.app.setView('profile')" style="display:flex; align-items:center; gap:10px; cursor:pointer;">
                            <div style="text-align:right; line-height:1.2;">
                                <div style="font-size:0.85rem; font-weight:600;">${Store.state.user.name}</div>
                                <div style="font-size:0.7rem; color:var(--text-muted);">${Store.state.user.position}</div>
                            </div>
                            <div class="avatar">${Store.state.user.name[0]}</div>
                        </div>
                    </div>
                </header>
                <div class="view-container">${this.renderCurrentView()}</div>
            </main>
        `;
    }

    getViewTitle() {
        const map = { dashboard: 'Dashboard', leads: 'Potansiyel Müşteri & Fırsat Yönetimi', customers: 'Müşteri Portföyü', calendar: 'Etkinlik Takvimi', detail: 'Firma Detayları', drafts: 'Mail Simülasyon Merkezi', profile: 'Profilim' };
        return map[this.currentView] || 'OYPA';
    }

    renderUpcomingShort() {
        const now = new Date();
        const upcoming = Store.state.meetings
            .filter(m => new Date(m.date) >= now && m.status === 'Planlandı')
            .sort((a,b) => new Date(a.date) - new Date(b.date))
            .slice(0, 3);
        
        if(!upcoming.length) return '<p style="color:var(--text-muted); font-size:0.7rem;">Planlanmış etkinlik yok.</p>';
        return upcoming.map(m => {
            const company = [...Store.state.leads, ...Store.state.customers].find(c => c.id === Number(m.companyId));
            return `<div style="padding:8px 0; border-bottom:1px solid rgba(255,255,255,0.05);">
                <strong>${company?.company || 'Firma'}</strong><br/>
                <span style="opacity:0.7;">${m.date} - ${m.time}</span>
            </div>`;
        }).join('');
    }

    renderCurrentView() {
        if (this.currentView === 'dashboard') return this.renderDashboard();
        if (this.currentView === 'leads') return this.renderLeads();
        if (this.currentView === 'customers') return this.renderCustomers();
        if (this.currentView === 'calendar') return this.renderCalendar();
        if (this.currentView === 'detail') return this.renderCompanyDetail(this.selectedId);
        if (this.currentView === 'profile') return this.renderProfile();
        if (this.currentView === 'drafts') return this.renderDrafts();
        return '';
    }

    renderDashboard() {
        const doneCount = Store.state.meetings.filter(m => m.status === 'Yapıldı').length;
        const totalTarget = Store.state.targets.weekly;
        const percent = Math.min(100, Math.round((doneCount/totalTarget)*100));

        return `
            <div class="dashboard-grid">
                <div class="stat-card glass" style="border-left: 4px solid var(--warning);" onclick="window.app.setView('leads')">
                    <span class="stat-label">Aktif Leadler</span>
                    <span class="stat-value">${Store.state.leads.length}</span>
                </div>
                <div class="stat-card glass" style="border-left: 4px solid var(--success);" onclick="window.app.setView('customers')">
                    <span class="stat-label">Toplam Müşteri</span>
                    <span class="stat-value">${Store.state.customers.length}</span>
                </div>
                <div class="stat-card glass" style="border-left: 4px solid var(--primary-light);" onclick="window.app.setView('calendar')">
                    <span class="stat-label">Planlı Ziyaretler</span>
                    <span class="stat-value">${Store.state.meetings.filter(m => m.status === 'Planlandı').length}</span>
                </div>
                
                <div class="stat-card glass full-width" style="border-top: 4px solid var(--accent-gold); flex-direction:row; justify-content:space-between; align-items:center;">
                    <div>
                        <h3 style="margin-bottom:10px;">Haftalık Hedef Takvimi</h3>
                        <p style="color:var(--text-muted)">Bu hafta yapılması planlanan <strong>${totalTarget}</strong> görüşmeden <strong>${doneCount}</strong> tanesi tamamlandı.</p>
                    </div>
                    <div style="text-align:right;">
                        <div style="font-size:2.5rem; font-weight:800; color:var(--accent-gold);">${percent}%</div>
                        <div style="width:250px; height:8px; background:rgba(255,255,255,0.05); border-radius:10px; margin-top:10px; overflow:hidden;">
                            <div style="width:${percent}%; height:100%; background:var(--accent-gold); box-shadow: 0 0 10px var(--accent-gold);"></div>
                        </div>
                    </div>
                </div>

                <div class="glass full-width" style="padding:24px; border-radius:16px;">
                    <h3>Haftalık Görüşme Yoğunluğu</h3>
                    <div class="chart-container" id="dashboard-chart">
                        ${this.renderChart()}
                    </div>
                </div>
            </div>`;
    }

    renderChart() {
        const days = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'];
        const values = [2, 5, 3, 8, 4, 1, 0]; // Mock sample
        const max = Math.max(...values, 10);
        return days.map((day, i) => {
            const height = (values[i] / max) * 100;
            const left = (i * (100 / 7)) + 5;
            return `
                <div class="chart-bar" style="height:${height}%; left:${left}%; width:40px;"></div>
                <div style="position:absolute; bottom:-30px; left:${left}%; width:40px; text-align:center; font-size:0.7rem; color:var(--text-muted);">${day}</div>
            `;
        }).join('');
    }

    renderLeads() {
        return `
            <div style="display:flex; justify-content:space-between; align-items:center; margin-top:20px;">
                <h3>Lead & Fırsat Listesi</h3>
                <button class="btn btn-primary" onclick="window.app.showModal('m-lead')">${Icons.plus} Yeni Firma/Fırsat</button>
            </div>
            <div class="data-table-container glass">
                <table class="data-table">
                    <thead><tr><th>Firma Ünvanı</th><th>Sektör</th><th>Adres</th><th>Durum</th><th>İşlem</th></tr></thead>
                    <tbody>
                        ${Store.state.leads.length ? Store.state.leads.map(l => `
                            <tr>
                                <td><strong>${l.company}</strong></td>
                                <td><span class="badge" style="background:rgba(255,255,255,0.1); color:white;">${l.sector}</span></td>
                                <td style="font-size:0.85rem; color:var(--text-muted);">${l.address}</td>
                                <td><span class="badge badge-lead">${l.status}</span></td>
                                <td><button class="btn btn-ghost btn-sm" onclick="window.app.setView('detail', ${l.id})">Detay / İşlem</button></td>
                            </tr>`).join('') : '<tr><td colspan="5" style="text-align:center; padding:40px; color:var(--text-muted);">Sistemde aktif lead bulunmuyor.</td></tr>'}
                    </tbody>
                </table>
            </div>
            ${this.renderLeadModal()}`;
    }

    renderLeadModal(prefillData = null) {
        return `
            <div id="m-lead" class="modal-overlay" style="display:none">
                <div class="modal-content glass" style="width:600px;">
                    <div class="modal-header"><h3>${prefillData ? 'Yeni Fırsat Ekle' : 'Yeni Firma Kaydı'}</h3><button class="btn btn-ghost btn-sm" onclick="window.app.closeModal('m-lead')">&times;</button></div>
                    <form onsubmit="event.preventDefault(); window.app.saveLead(this);" class="crm-form">
                        <div class="form-group">
                            <label>Firma Ünvanı</label>
                            <input name="company" required value="${prefillData?.company || ''}">
                            ${prefillData ? `<div class="checkbox-group"><input type="checkbox" checked disabled> <span>${prefillData.company} ile aynı</span></div>` : ''}
                        </div>
                        <div class="form-row">
                            <div class="form-group">
                                <label>Sektör</label>
                                <select name="sector" required>
                                    <option value="" disabled selected>Şu Sektör İçin:</option>
                                    <option value="Turizm">Turizm</option>
                                    <option value="Perakende">Perakende</option>
                                    <option value="Tesis Yönetimi">Tesis Yönetimi</option>
                                    <option value="Diğer">Diğer</option>
                                </select>
                            </div>
                            <div class="form-group">
                                <label>Telefon</label>
                                <input name="phone" required value="${prefillData?.phone || ''}">
                                ${prefillData ? `<div class="checkbox-group"><input type="checkbox" onchange="window.app.handlePrefill(this, 'phone', '${prefillData.phone}')"> <span>Mevcutla aynı</span></div>` : ''}
                            </div>
                        </div>
                        <div class="form-group">
                            <label>Email</label>
                            <input name="email" required value="${prefillData?.email || ''}">
                            ${prefillData ? `<div class="checkbox-group"><input type="checkbox" onchange="window.app.handlePrefill(this, 'email', '${prefillData.email}')"> <span>Mevcutla aynı</span></div>` : ''}
                        </div>
                        <div class="form-group">
                            <label>Adres</label>
                            <input name="address" required value="${prefillData?.address || ''}">
                            ${prefillData ? `<div class="checkbox-group"><input type="checkbox" onchange="window.app.handlePrefill(this, 'address', '${prefillData.address}')"> <span>Mevcutla aynı</span></div>` : ''}
                        </div>
                        <div class="modal-footer" style="display:flex; justify-content:flex-end; gap:10px; margin-top:20px;">
                            <button type="button" class="btn btn-ghost" onclick="window.app.closeModal('m-lead')">İptal</button>
                            <button type="submit" class="btn btn-primary">Kaydet</button>
                        </div>
                    </form>
                </div>
            </div>`;
    }

    handlePrefill(cb, fieldName, value) {
        const input = cb.closest('.form-group').querySelector('input');
        if (cb.checked) {
            input.value = value;
            input.readOnly = true;
        } else {
            input.readOnly = false;
        }
    }

    saveLead(form) { 
        const data = Object.fromEntries(new FormData(form));
        Store.addLead(data); 
        this.closeModal('m-lead');
        this.render();
    }

    renderCustomers() {
        return `
            <div style="margin-top:20px;"><h3>Aktif Müşteri Portföyü</h3></div>
            <div class="data-table-container glass">
                <table class="data-table">
                    <thead><tr><th>Firma</th><th>Sektör</th><th>Aktif Geçiş Tarihi</th><th>İletişim</th><th>İşlem</th></tr></thead>
                    <tbody>
                        ${Store.state.customers.length ? Store.state.customers.map(c => `
                            <tr>
                                <td><strong>${c.company}</strong></td>
                                <td><span class="badge" style="background:var(--primary-light); color:white;">${c.sector}</span></td>
                                <td style="font-size:0.85rem;">${c.activeDate || '-'}</td>
                                <td style="font-size:0.85rem;">${c.email}</td>
                                <td><button class="btn btn-ghost btn-sm" onclick="window.app.setView('detail', ${c.id})">Dosyayı Aç</button></td>
                            </tr>`).join('') : '<tr><td colspan="5" style="text-align:center; padding:40px; color:var(--text-muted);">Henüz aktif müşterimiz bulunmuyor.</td></tr>'}
                    </tbody>
                </table>
            </div>`;
    }

    renderCompanyDetail(id) {
        const company = [...Store.state.leads, ...Store.state.customers].find(c => c.id === id);
        if(!company) return '<div style="padding:40px; text-align:center;">Firma bulunamadı.</div>';
        
        const contacts = Store.state.contacts.filter(c => Number(c.companyId) === id);
        const meetings = Store.state.meetings.filter(m => Number(m.companyId) === id);

        return `
            <div class="detail-page" style="display:grid; grid-template-columns: 1fr 2fr; gap:24px; margin-top:20px;">
                <div style="display:flex; flex-direction:column; gap:20px;">
                    <div class="glass" style="padding:24px; border-radius:16px;">
                        <h3 style="margin-bottom:10px;">${company.company}</h3>
                        <p style="color:var(--text-muted); font-size:0.9rem;">${company.sector}</p>
                        <div style="margin-top:15px;">
                            ${company.type === 'customer' ? '<span class="badge badge-customer">Aktif Müşteri</span>' : '<span class="badge badge-lead">Lead / Potansiyel</span>'}
                        </div>
                        <hr style="opacity:0.1; margin:20px 0;">
                        <div style="font-size:0.85rem; display:flex; flex-direction:column; gap:12px;">
                            <span>📍 <strong>Adres:</strong> <br/>${company.address}</span>
                            <span>📧 <strong>E-Mail:</strong> <br/>${company.email}</span>
                            <span>📞 <strong>Telefon:</strong> <br/>${company.phone}</span>
                        </div>
                        <div style="margin-top:30px; display:flex; flex-direction:column; gap:10px;">
                            ${company.type !== 'customer' ? `<button class="btn btn-primary" onclick="window.app.changeToCustomer(${id})">Müşteriye Dönüştür</button>` : ''}
                            <button class="btn btn-ghost" onclick="window.app.openOpportunityModal(${id})">${Icons.plus} Add New Opportunity</button>
                            <button class="btn btn-ghost" onclick="window.app.openMeetingModal(${id})">📅 Randevu Planla</button>
                        </div>
                    </div>
                </div>
                <div style="display:flex; flex-direction:column; gap:24px;">
                    <div class="glass" style="padding:24px; border-radius:16px;">
                        <div style="display:flex; justify-content:space-between; align-items:center; margin-bottom:20px;">
                            <h4>İlgili Kişiler (Contacts)</h4>
                            <button class="btn btn-ghost btn-sm" onclick="window.app.showModal('m-contact')">+ Yeni Kişi</button>
                        </div>
                        <div style="display:grid; grid-template-columns:1fr 1fr; gap:15px;">
                            ${contacts.length ? contacts.map(c => `
                                <div class="glass" style="padding:15px; border-radius:12px; font-size:0.85rem;">
                                    <div style="font-weight:700; margin-bottom:5px;">${c.name}</div>
                                    <div style="opacity:0.6;">${c.email}</div>
                                    <div style="opacity:0.6;">${c.phone}</div>
                                </div>`).join('') : '<p style="color:var(--text-muted); font-size:0.85rem;">Henüz kontak eklenmemiş.</p>'}
                        </div>
                    </div>
                    <div class="glass" style="padding:24px; border-radius:16px;">
                        <h4>Görüşme Kayıtları</h4>
                        <div class="data-table-container" style="background:none; border:none; margin-top:15px;">
                            <table class="data-table" style="font-size:0.85rem;">
                                <thead><tr><th>Tarih</th><th>Temsilci</th><th>Yöntem</th><th>Durum</th><th>İşlem</th></tr></thead>
                                <tbody>
                                    ${meetings.map(m => {
                                        const rep = Store.state.reps.find(r => r.id === Number(m.repId));
                                        return `<tr>
                                            <td>${m.date}<br/>${m.time}</td>
                                            <td>${rep?.name || 'Bilinmiyor'}</td>
                                            <td style="text-transform:capitalize;">${m.method}</td>
                                            <td><span class="badge ${m.status === 'Yapıldı' ? 'badge-customer' : 'badge-lead'}" style="font-size:0.65rem;">${m.status}</span></td>
                                            <td>
                                                ${m.status === 'Planlandı' ? `<button class="btn btn-ghost btn-sm" onclick="window.app.updateM(${m.id}, 'Yapıldı')">Yapıldı</button>` : (m.comment ? `${Icons.check} <span style="font-size:0.7rem; opacity:0.6;">Okundu</span>` : '-')}
                                            </td>
                                        </tr>`;
                                    }).join('')}
                                    ${!meetings.length ? '<tr><td colspan="5" style="text-align:center; opacity:0.5;">Kayıt yok.</td></tr>' : ''}
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>
            ${this.renderContactModal(id)}
            ${this.renderMeetingModal(id, company)}
            <div id="opp-modal-container"></div>
        `;
    }

    openOpportunityModal(id) {
        const company = [...Store.state.leads, ...Store.state.customers].find(c => c.id === id);
        const container = document.getElementById('opp-modal-container');
        container.innerHTML = this.renderLeadModal(company);
        this.showModal('m-lead');
    }

    renderContactModal(id) {
        return `
        <div id="m-contact" class="modal-overlay" style="display:none">
            <div class="modal-content glass">
                <div class="modal-header"><h3>Yeni Kontak Ekle</h3></div>
                <form onsubmit="event.preventDefault(); window.app.saveContact(this, ${id});" class="crm-form">
                    <input name="companyId" type="hidden" value="${id}">
                    <div class="form-group"><label>İsim Soyisim</label><input name="name" required></div>
                    <div class="form-group"><label>E-Mail</label><input type="email" name="email"></div>
                    <div class="form-group"><label>Telefon</label><input name="phone"></div>
                    <div class="modal-footer" style="display:flex; justify-content:flex-end; gap:10px;"><button type="button" class="btn btn-ghost" onclick="window.app.closeModal('m-contact')">İptal</button><button type="submit" class="btn btn-primary">Kaydet</button></div>
                </form>
            </div>
        </div>`;
    }

    renderMeetingModal(id, company = null) {
        const contacts = Store.state.contacts.filter(c => Number(c.companyId) === Number(id));
        const companies = company ? [company] : [...Store.state.leads, ...Store.state.customers];
        return `
        <div id="m-meeting" class="modal-overlay" style="display:none">
            <div class="modal-content glass" style="width:600px;">
                <div class="modal-header"><h3>Randevu / Görüşme Planla</h3></div>
                <form onsubmit="event.preventDefault(); window.app.saveMeeting(this);" class="crm-form">
                    <div class="form-group">
                        <label>Hedef Firma</label>
                        <select name="companyId" required ${company ? 'readonly' : ''} onchange="window.app.updateContactList(this.value)">
                            ${companies.map(c => `<option value="${c.id}">${c.company} (${c.sector})</option>`).join('')}
                        </select>
                    </div>
                    <div class="form-group">
                        <label>Firma Temsilcisi</label>
                        <select name="contactId" id="contactSelect" required>
                            <option value="">Seçiniz</option>
                            ${contacts.map(c => `<option value="${c.id}">${c.name}</option>`).join('')}
                        </select>
                    </div>
                    <div class="form-row">
                        <div class="form-group">
                            <label>OYPA Temsilcisi</label>
                            <select name="repId" required>
                                ${Store.state.reps.map(r => `<option value="${r.id}">${r.name}</option>`).join('')}
                            </select>
                        </div>
                        <div class="form-group">
                            <label>Yöntem</label>
                            <select name="method">
                                <option value="visit">Yüz Yüze Ziyaret</option>
                                <option value="phone">Telefon Görüşmesi</option>
                                <option value="email">E-mail / Teklif</option>
                            </select>
                        </div>
                    </div>
                    <div class="form-row">
                        <div class="form-group"><label>Tarih</label><input type="date" name="date" required value="${this.tempDate || ''}"></div>
                        <div class="form-group"><label>Saat</label><input type="time" name="time" required></div>
                    </div>
                    <div class="form-group"><label>Görüşme Adresi</label><input name="address" required value="${company?.address || ''}"></div>
                    <div class="modal-footer" style="display:flex; justify-content:flex-end; gap:10px;"><button type="button" class="btn btn-ghost" onclick="window.app.closeModal('m-meeting')">İptal</button><button type="submit" class="btn btn-primary">Kaydet ve Mail Oluştur</button></div>
                </form>
            </div>
        </div>`;
    }

    saveContact(form, id) { 
        Store.addContact(Object.fromEntries(new FormData(form))); 
        this.render(); 
    }

    saveMeeting(form) {
        const d = Object.fromEntries(new FormData(form));
        const draft = Store.scheduleMeeting(d);
        alert(`Randevu kaydedildi!\n\nHatırlatma maili taslağı oluşturuldu: ${draft.subject}`);
        this.tempDate = null;
        this.closeModal('m-meeting');
        this.render();
    }

    changeToCustomer(id) {
        if(confirm('Firmayı aktif portföye taşımak istediğinizden emin misiniz?')) {
            Store.convertLeadToCustomer(id);
            this.setView('customers');
        }
    }

    updateM(id, status) { Store.updateMeetingStatus(id, status); this.render(); }

    renderCalendar() {
        const now = new Date();
        const month = now.toLocaleString('tr-TR', { month: 'long', year: 'numeric' });
        return `
            <div style="margin-top:20px; display:flex; justify-content:space-between; align-items:center;">
                <div>
                    <h3>Ziyaret Takvimi</h3>
                    <p style="color:var(--text-muted); font-size:0.9rem;">Planlarınızı yönetmek için günlere tıklayın.</p>
                </div>
                <button class="btn btn-primary" onclick="window.app.openMeetingModal()">${Icons.plus} Yeni Randevu</button>
            </div>
            <div class="calendar-layout">
                <div class="calendar-grid glass">
                    <div class="cal-header">${month}</div>
                    <div class="cal-days">
                        ${['Pzt','Sal','Çar','Per','Cum','Cmt','Paz'].map(d => `<div style="text-align:center; font-weight:700; padding-bottom:15px; color:var(--text-muted);">${d}</div>`).join('')}
                        ${Array.from({length: 31}, (_, i) => {
                            const day = i+1;
                            const dateStr = `2026-03-${day.toString().padStart(2,'0')}`;
                            const hasMeeting = Store.state.meetings.some(m => m.date === dateStr);
                            return `<div class="cal-cell ${day===17?'today':''} ${hasMeeting?'has-event':''}" onclick="window.app.handleDateClick('${dateStr}')">${day}</div>`;
                        }).join('')}
                    </div>
                </div>
                <div class="glass" style="padding:24px; border-radius:16px;">
                    <h4>Seçili Gün Etkinlikleri</h4>
                    <div style="margin-top:20px; display:flex; flex-direction:column; gap:10px;">
                        ${this.renderDayEvents()}
                    </div>
                </div>
            </div>
            ${this.renderMeetingModal()}`;
    }

    handleDateClick(date) {
        this.tempDate = date;
        this.render();
        // Optional: Auto open modal if user wants
    }

    renderDayEvents() {
        const date = this.tempDate || '2026-03-17';
        const dayMeetings = Store.state.meetings.filter(m => m.date === date);
        if(!dayMeetings.length) return `<p style="font-size:0.85rem; color:var(--text-muted);">Bu tarihte planlı görüşme yok.</p><button class="btn btn-ghost btn-sm" style="width:100%" onclick="window.app.openMeetingModal()">+ Ekle</button>`;
        return dayMeetings.map(m => {
            const company = [...Store.state.leads, ...Store.state.customers].find(c => c.id === Number(m.companyId));
            return `<div class="glass" style="padding:12px; border-radius:8px; border-left:3px solid var(--accent-gold);">
                <div style="font-weight:700; font-size:0.85rem;">${company?.company}</div>
                <div style="font-size:0.75rem; opacity:0.7;">${m.time} - ${m.method}</div>
            </div>`;
        }).join('') + `<button class="btn btn-ghost btn-sm" style="width:100%" onclick="window.app.openMeetingModal()">+ Başka Ekle</button>`;
    }

    renderDrafts() {
        return `
            <div style="margin-top:20px;">
                <h3>Mail Simülasyon Merkezi</h3>
                <p style="color:var(--text-muted); font-size:0.9rem;">Sistem tarafından oluşturulan otomatik hatırlatma maillerini buradan yönetebilirsiniz.</p>
            </div>
            <div class="data-table-container glass">
                <table class="data-table">
                    <thead><tr><th>Oluşturulma</th><th>Alıcı</th><th>Konu</th><th>Durum</th><th>İşlem</th></tr></thead>
                    <tbody>
                        ${Store.state.mailDrafts.length ? Store.state.mailDrafts.map(d => `
                            <tr>
                                <td style="font-size:0.8rem;">${d.date}</td>
                                <td><strong>${d.to}</strong></td>
                                <td style="font-size:0.85rem;">${d.subject}</td>
                                <td>${d.sent ? '<span class="badge badge-customer">GÖNDERİLDİ</span>' : '<span class="badge badge-lead">BEKLEMEDE</span>'}</td>
                                <td>
                                    <button class="btn btn-ghost btn-sm" onclick="alert('İçerik:\\n\\n${d.body.replace(/\n/g, '\\n')}')">Görüntüle</button>
                                    ${!d.sent ? `<button class="btn btn-primary btn-sm" onclick="window.app.sendDraft(${d.id})">Simüle Gönder</button>` : ''}
                                </td>
                            </tr>`).join('') : '<tr><td colspan="5" style="text-align:center; padding:40px;">Henüz taslak oluşturulmamış.</td></tr>'}
                    </tbody>
                </table>
            </div>`;
    }

    sendDraft(id) { 
        Store.simulateSendDraft(id); 
        this.render(); 
    }

    renderProfile() {
        return `
            <div class="glass" style="margin-top:20px; padding:40px; border-radius:20px; text-align:center; max-width:600px;">
                <div class="avatar" style="width:100px; height:100px; font-size:3rem; margin: 0 auto 20px;">${Store.state.user.name[0]}</div>
                <h2>${Store.state.user.name}</h2>
                <p style="color:var(--accent-gold); margin-bottom:20px;">${Store.state.user.position}</p>
                <div style="text-align:left; background:rgba(0,0,0,0.2); padding:20px; border-radius:12px; font-size:0.9rem; display:flex; flex-direction:column; gap:10px;">
                    <span>📧 <strong>E-Mail:</strong> ${Store.state.user.email}</span>
                    <span>📞 <strong>Telefon:</strong> ${Store.state.user.phone}</span>
                </div>
            </div>`;
    }

    // Modal Helpers
    showModal(id) { document.getElementById(id).style.display = 'flex'; }
    closeModal(id) { document.getElementById(id).style.display = 'none'; }
    openMeetingModal(id) { 
        if(id) this.selectedId = id;
        this.showModal('m-meeting'); 
    }

    updateContactList(companyId) {
        const select = document.getElementById('contactSelect');
        const contacts = Store.state.contacts.filter(c => Number(c.companyId) === Number(companyId));
        select.innerHTML = '<option value="">Seçiniz</option>' + contacts.map(c => `<option value="${c.id}">${c.name}</option>`).join('');
    }

    showNotifications() {
        alert("BİLDİRİMLER:\n\n" + Store.state.notifications.map(n => `- ${n.time}: ${n.message}`).join('\n'));
    }
}

window.app = new App();
