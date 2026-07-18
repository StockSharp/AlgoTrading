# Donchian Escalpelador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Donchian Scalper é uma versão StockSharp do MetaTrader 4 consultor especialista `DonchianScalperEA`. A estratégia monitora Donchian limites de canal e a média móvel exponencial (EMA) de mesmo comprimento. Uma ordem de stop pendente é armada somente depois que o preço recua no EMA, sinalizando que o impulso foi redefinido antes de um possível rompimento. As entradas são executadas com ordens de stop colocadas nos extremos atuais Donchian e protegidas pela banda oposta. Os lucros são gerenciados por uma distância fixa de lucro ou por trailing stops adaptativos que acompanham a estrutura de mercado escolhida.

## Lógica estratégica
### Preparação de entrada
* **Validação de pullback** – a estratégia espera até que uma das duas velas fechadas anteriormente cruze abaixo de EMA (para posições compradas) ou acima de EMA (para posições vendidas). O nível de cruzamento é compensado pela distância configurável *Cross Anchor* para garantir que o recuo seja significativo.
* **Armação de breakout** – assim que a condição de pullback for satisfeita e o cronômetro de resfriamento expirar, uma ordem de stop é enviada no limite Donchian mais recente (faixa superior para posições compradas, faixa inferior para posições vendidas). A banda oposta define a parada protetora inicial. As ordens pendentes existentes são realinhadas automaticamente quando os níveis Donchian se estabilizam por pelo menos duas velas.

### Gestão comercial
* **Proteção inicial** – quando uma ordem de rompimento é preenchida, a estratégia coloca uma ordem stop de proteção usando o preço stop pré-calculado. O nível de stop é igual à banda Donchian oposta e pode ser deslocado para dentro pela configuração *Stop Loss (pontos)*.
* **Controle de lucro** – dois modos de gerenciamento estão disponíveis:
  * *Close At Profit* – fecha a posição quando o movimento líquido do preço médio de entrada excede a distância de take-profit configurada.
  * *Trailing* – mantém a negociação aberta e aperta periodicamente o stop de proteção. O mecanismo final pode seguir o limite Donchian, o EMA ou uma banda de volatilidade baseada em ATR.
* **Cooldown** – após o fechamento de todas as posições, a estratégia aguarda o número especificado de velas finalizadas antes de armar novas ordens de rompimento. Isso reproduz a lógica MetaTrader que requer pelo menos três barras entre negociações.

## Parâmetros
* **Volume** – volume de ordens usado para entradas de stop e saídas de mercado.
* **Período do canal** – Donchian duração do canal, também usado para o filtro EMA.
* **Cross Anchor** – distância adicional (em pontos) que o recuo deve exceder antes que a ordem de fuga seja armada.
* **Stop Loss (pontos)** – distância somada à banda oposta Donchian para o stop de proteção inicial; defina como `0` para colocar a parada diretamente na banda.
* **Take Profit (pontos)** – meta de lucro usada pela modalidade *Close At Profit*. Ignorado quando o modo de rastreamento está ativo.
* **Tipo de vela** – cálculos do indicador de condução do período.
* **Modo Lucro** – seleciona entre saída fixa de lucro e trailing stops adaptativos.
* **Modo Trailing** – mecanismo de trailing usado no modo de lucro *Trailing*. As opções são limite Donchian, EMA ou rastreamento baseado em ATR.
* **Barras de Resfriamento** – número mínimo de velas concluídas que devem passar depois que a posição se torna plana antes que novas ordens possam ser feitas.
* **ATR Período / ATR Multiplicador** – parâmetros para o mecanismo de rastreamento ATR. O multiplicador define quantos ATRs são subtraídos (longos) ou adicionados (curtos) para calcular o trailing stop.

## Notas adicionais
* A estratégia alinha cada preço de stop e de entrada com a etapa de preço do instrumento para garantir a conformidade cambial.
* Quando as ordens stop longas e curtas estão ativas, o preenchimento de um lado cancelará automaticamente a ordem pendente oposta para evitar hedge.
* Se *Take Profit (pontos)* for definido como zero enquanto o modo de lucro permanecer *Close At Profit*, a estratégia manterá as posições abertas até que o stop de proteção seja atingido.
* A conversão se concentra no StockSharp API de alto nível: vinculação de indicadores, assinaturas de velas e métodos auxiliares (`BuyStop`, `SellStop`, `SellMarket`, etc.). A implementação do Python não está incluída neste pacote.
