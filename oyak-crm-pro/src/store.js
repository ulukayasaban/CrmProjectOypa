export const Store = {
    state: {
        user: { name: 'Helin Zin Ası', email: 'hasi@oypa.com.tr', position: 'Marketing & Sales Intern', phone: '+90 5XX XXX XX XX' },
        reps: [
            { id: 1, name: 'Helin Zin Ası', email: 'hasi@oypa.com.tr' },
            { id: 2, name: 'Hasan Aksoy', email: 'hasan.aksoy@oypa.com' },
            { id: 3, name: 'Ayşe Yılmaz', email: 'ayse.yilmaz@oypa.com' }
        ],
        leads: [
            { id: 1, type: 'lead', company: 'Global Enerji A.Ş.', address: 'Ankara, Çankaya No:1', sector: 'Enerji', phone: '0212 111 22 33', email: 'info@global.com', status: 'Yeni' },
        ],
        customers: [],
        contacts: [], // { id, companyId, name, email, phone }
        meetings: [], // { id, companyId, contactId, repId, date, time, address, method, status, comment }
        targets: { weekly: 5 }, 
        notifications: [],
        mailDrafts: [] // { id, to, subject, body, sent: false }
    },
    listeners: [],
    init() {
        const saved = localStorage.getItem('oypa_crm_data_v4');
        if (saved) {
            try {
                this.state = { ...this.state, ...JSON.parse(saved) };
            } catch (e) {
                console.error("Store init error:", e);
            }
        }
        this.notify();
    },
    save() {
        localStorage.setItem('oypa_crm_data_v4', JSON.stringify(this.state));
        this.notify();
    },
    subscribe(fn) { this.listeners.push(fn); },
    notify() { this.listeners.forEach(fn => fn(this.state)); },
    
    addLead(lead) {
        this.state.leads.push({ 
            ...lead, 
            id: Date.now(), 
            status: 'Yeni',
            createdAt: new Date().toISOString().split('T')[0]
        });
        this.addNotification(`${lead.company} (${lead.sector}) sisteme lead olarak eklendi.`);
        this.save();
    },
    
    addContact(contact) {
        this.state.contacts.push({ ...contact, id: Date.now() });
        this.save();
    },

    addRep(rep) {
        const newRep = { ...rep, id: Date.now() };
        this.state.reps.push(newRep);
        this.save();
        return newRep;
    },

    scheduleMeeting(meeting) {
        const m = { ...meeting, id: Date.now(), status: 'Planlandı', comment: '' };
        this.state.meetings.push(m);
        
        // Create Mail Draft
        const rep = this.state.reps.find(r => r.id === Number(m.repId));
        const company = [...this.state.leads, ...this.state.customers].find(c => c.id === Number(m.companyId));
        const contact = this.state.contacts.find(c => c.id === Number(m.contactId));
        
        const draft = {
            id: Date.now() + 1,
            to: rep?.email,
            subject: `${company?.company} ile Yaklaşan Etkinlik!`,
            body: `Sayın ${rep?.name},\n\n${company?.company} ile yaklaşan etkinlik!\n\nDetaylar:\nFirma Temsilcisi: ${contact?.name || 'Belirtilmedi'}\nTarih: ${m.date}\nSaat: ${m.time}\nAdres: ${m.address}\nYöntem: ${m.method}`,
            sent: false,
            date: new Date().toLocaleString()
        };
        this.state.mailDrafts.unshift(draft);
        
        this.addNotification(`${m.date} tarihli görüşme planlandı ve mail taslağı oluşturuldu.`);
        this.save();
        return draft;
    },

    updateMeetingStatus(id, status, comment = '') {
        const m = this.state.meetings.find(x => x.id === id);
        if (m) {
            m.status = status;
            m.comment = comment;
            this.save();
        }
    },

    convertLeadToCustomer(leadId) {
        const idx = this.state.leads.findIndex(l => l.id === leadId);
        if (idx !== -1) {
            const lead = this.state.leads[idx];
            this.state.customers.push({ 
                ...lead, 
                type: 'customer', 
                status: 'Aktif',
                activeDate: new Date().toISOString().split('T')[0]
            });
            this.state.leads.splice(idx, 1);
            this.addNotification(`${lead.company} artık aktif bir müşterimiz! 🎊`);
            this.save();
        }
    },

    addNotification(message) {
        this.state.notifications.unshift({ 
            id: Date.now(), 
            message, 
            time: new Date().toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' }) 
        });
        this.notify();
    },

    simulateSendDraft(id) {
        const draft = this.state.mailDrafts.find(d => d.id === id);
        if (draft) {
            draft.sent = true;
            this.addNotification(`Hatırlatma maili ${draft.to} adresine simüle olarak gönderildi.`);
            this.save();
        }
    }
};
