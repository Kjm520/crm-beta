
search fields are still placeholders



The native windows delete prompt modal is very ugly
  [Completed] Replaced the 4 delete confirmations (contact, company, lead, lead status) with a
  themed ConfirmWindow (Views/Dialogs/) + new ButtonDanger style.
  [Completed] Then converted every other native dialog too: ConfirmWindow.Alert() (one-button)
  now covers the validation notices, backup success/failure, and the lead-converted notice, and
  ConfirmWindow.Ask(destructive:false) covers the lead-convert confirm. No MessageBox.Show calls
  remain anywhere in the app.

the new Location field is way too wide

