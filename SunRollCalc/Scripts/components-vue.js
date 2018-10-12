//<!-- template for the modal component -->
 //<script type="text/x-template" id="modal-template">
	 <transition name="modal">
		 <div class="modal-mask">
			 <div class="modal-wrapper">
				 <div class="modal-container">

					 <div class="modal-header">
						 <slot name="header">Смета:</slot>
					 </div>

					 <div class="modal-body">
						 <slot name="body" class="container-fluid">
	//   		 @*<div v-for="item in items" class="row">
	//    <div class="col-xs-6" style="background-color:lavender;">{{ item.Caption }}</div>
	//    <div class="col-xs-6" style="background-color:lavenderblush;">{{ item.Price }} руб.</div>
	//</div>*@
	//   		 @*<form id="send-smeta">
	//    <div class="row"><label for="email">Ф.И.О.</label><input v-model="fio" id="fio"></div>
	//    <div class="row"><label for="email">E-mail</label><input v-model="email" id="email"></div>
	//    <div class="row"><label for="email">Телефон</label><input v-model="phone" id="phone"></div>
	//</form>*@
							 <form id="send-smeta" @submit.prevent="sendEmail">
								 <table>
									 <tr><td><label for="email">Ф.И.О.</label></td><td><input v-model="fio" id="fio"></td></tr>
									 <tr><td><label for="email">E-mail</label></td><td><input v-model="email" id="email"></td></tr>
									 <tr><td><label for="email">Телефон</label></td><td><input v-model="phone" id="phone"></td></tr>
								 </table>
								 <button class="modal-default-button" type="submit">
									 ЗАКАЗАТЬ
								 </button>
							 </form>
						 </slot>
					 </div>

					 <strong>{{ status }}</strong>

					 <div class="modal-footer">
						 <slot name="footer">
							 <strong>{{ message }}</strong>
							 
							 <button class="modal-default-button" @click="$emit('close')">
		 OK
	 </button>
						 </slot>
					 </div>
				 </div>
			 </div>
		 </div>
	 </transition>
 //</script>