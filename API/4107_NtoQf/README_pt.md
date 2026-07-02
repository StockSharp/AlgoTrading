# Multifiltro NTOqF
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia NTOqF Multi-Filter transporta o MetaTrader 4 consultor especialista "NTOqF" (versões V1 – V3) para o StockSharp de alto nível API. O robô original combina vários osciladores e filtros de acompanhamento de tendências, cada um dos quais pode ser ativado ou desativado de forma independente. Esta versão C# preserva a mesma configurabilidade, oferece suporte a prazos separados para cada indicador e aplica gerenciamento comercial por meio de paradas fixas, metas de lucro e um trailing stop opcional expresso em pips.

## Lógica estratégica
### Filtros indicadores
* **RSI filtro** – gera um sinal longo quando o valor de RSI (no turno configurado) estiver abaixo de `RSI Lower` e um sinal curto quando o valor estiver acima de `RSI Upper`. Leituras neutras cancelam entradas.
* **Stochastic filtro** – compara %K e %D. Quando `Use Stochastic High/Low` está ativado, a linha principal também deve estar acima de `Stoch High` para posições longas ou abaixo de `Stoch Low` para posições curtas; caso contrário, cruzamentos simples de %K/%D serão usados.
* **ADX filtro** – usa +DI versus –DI para determinar a direção. Quando a opção `Use ADX Main` está habilitada, a linha principal ADX deve exceder `ADX Main` antes que qualquer entrada seja aceita.
* **Parabolic SAR filtro** – interpreta o valor SAR relativo ao fechamento da barra selecionada. Valores acima do preço favorecem as posições compradas (refletindo o comportamento no código MQL), valores abaixo favorecem as posições vendidas.
* **Filtro de média móvel** – compara a média móvel selecionada (com mudança positiva opcional) com o preço de fechamento na mudança base. Preço acima da MM favorece posições compradas; preço abaixo favorece shorts.

Todos os filtros habilitados devem concordar na mesma direção. Se algum filtro retornar um estado neutro (por exemplo, RSI permanecendo entre seus limites), nenhuma posição será aberta.

### Regras de entrada
* Os sinais são avaliados no período de negociação principal (`Candle Type`).
* Só é permitida uma posição por vez; a estratégia espera o fechamento da posição anterior antes de entrar em uma nova.
* O volume do pedido é obtido de `Trade Volume` (lotes).

### Regras de saída
* **Stop Loss/Take Profit fixos** – expresso em pips e convertido em compensações de preço usando o tamanho do passo do instrumento. Defina um parâmetro como `0` para desativar o nível correspondente.
* **Trailing stop** – quando ativado, o stop é seguido quando o lucro não realizado excede a distância final e o stop atual fica atrás do preço em mais do que essa distância. As posições longas movem o stop para cima, as posições curtas o movem para baixo.

### Comportamento multi-período
Cada indicador pode assinar seu próprio cronograma. Um valor de período de tempo de `0` reutiliza o período de negociação principal, enquanto valores positivos representam assinaturas de `TimeFrameCandle` baseadas em minutos. Os valores dos indicadores são avaliados apenas em velas concluídas e respeitam o parâmetro `Shift` para que a estratégia possa espelhar o comportamento de "retrospectiva" do especialista MetaTrader original.

## Parâmetros
* **Tipo de vela** – período de negociação usado para impulsionar as execuções.
* **Volume** – volume de ordens de mercado (lotes).
* **Take Profit (pips)** – meta de lucro; `0` desativa.
* **Stop Loss (pips)** – stop de proteção; `0` desativa.
* **Usar Trailing** / **Trailing Stop (pips)** – habilite e dimensione o trailing stop.
* **Shift** – número de velas concluídas ao ler os valores do indicador e preço.
* **RSI parâmetros** – alternância, período, limites superior/inferior e período.
* **Stochastic parâmetros** – alternância, %K/%D/comprimentos de desaceleração, níveis de confirmação altos/baixos opcionais e período de tempo.
* **ADX parâmetros** – alternância, período, período de DI, limite de linha principal opcional e período principal.
* **Parabolic SAR parâmetros** – alternância, etapa de aceleração, aceleração máxima e período de tempo.
* **Parâmetros de média móvel** – alternância, período, mudança adicional aplicada ao buffer MA, método de média (SMA/EMA/SMMA/LWMA), preço aplicado e período de tempo.

## Notas
* As filas de indicadores respeitam o `Shift` configurado, garantindo que os sinais sejam baseados em valores históricos da mesma forma que o especialista MQL.
* A lógica de trailing só é ativada quando a negociação já está lucrando mais do que a distância de trailing e o stop está a mais do que essa distância do preço, correspondendo ao comportamento do EA original.
* Nenhuma versão Python é fornecida para este pacote de estratégia.
