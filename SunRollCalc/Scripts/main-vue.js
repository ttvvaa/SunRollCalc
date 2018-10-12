<script>
	 Vue.component('modal', {
	 	props: ['items', 'message'],
	 	data() {
		   return { fio: '', email: '', phone: '', body: '', status: '' }
},
template: '#modal-template',
	methods: {
		sendEmail() {
			axios.post('/Calc/SendEmail', {
				fio: this.fio,
				email: this.email,
				phone: this.phone,
				body: 'body'
			}).then(response => {
				this.status = response.data.Status;
		})
		    }
}
});

new Vue({
	el: '#app',
	data() {
return {
	items : [ ],
message: '0',
width: '2000',
height: '1000',
mounting: 'int',
control: 'manual',
lock: '',
emergencyopen: '',
remotecontrol: '',
ismounting: '',
isdelivery: '',
kmdelivery: 0,
showModal: false,
fio: '', email: '', phone: '', body: '', status: ''
}
},
methods: {
	submitForm() {
		axios.post('/Calc/CalcPolicarbon', {
			width: this.width,
			height: this.height,
			mounting: this.mounting,
			control: this.control,
			lock:  this.lock,
			emergencyopen:  this.emergencyopen,
			remotecontrol:  this.remotecontrol,
			ismounting:  this.ismounting,
			isdelivery:  this.isdelivery,
			kmdelivery: this.kmdelivery
		}).then(response => {
			//this.message = JSON.stringify(response, null, 2)
			this.message = response.data.Price;
		this.items = response.data.Estimate;
	})
		    }
            
}
})
</script>