#property copyright "Direito Autoral \x00A9 2022, Fernando M. I. Carreiro, Todos os direitos reservados"
#property link      "https://www.mql5.com/en/users/FMIC"
#property version   "1.001"
#property strict

// Manipulador padrão de evento de tick
   void OnTick()
   {
      // Verifiquar a existencia duma nova barra (compatível com MQL4 e MQL5).
         static datetime dtBarraCorrente   = WRONG_VALUE;
                datetime dtBarraPrecedente = dtBarraCorrente;
                         dtBarraCorrente   = iTime( _Symbol, _Period, 0 );
                bool     bEventoBarraNova  = ( dtBarraCorrente != dtBarraPrecedente );

      // Reajir ao evento duma barra nova e lidar com a situação.
         if( bEventoBarraNova )
         {
            // Detectar se este é o primeiro tick recebido e lidar com a situação.
               /* Por exemplo, quando é aplicado pela primeira vez ao gráfico e
                  a barra está algures a meio do seu progresso e
                  não é realmente o início de uma nova barra. */
               if( dtBarraPrecedente == WRONG_VALUE )
               {
                  // Fazer algo no primeiro tick ou no meio duma barra ...
               }
               else
               {
                  // Fazer algo quando uma barra normal surgir ...
               };

            // Fazer algo independente da condição anterior ...
         }
         else
         {
            // Fazer outra coisa ...
         };

      // Fazer outras coisas ...
   };
