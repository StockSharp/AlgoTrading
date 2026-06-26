# Estratégia de DynamicRS_C
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o assessor especialista do MetaTrader **Exp_DynamicRS_C** usando a API de alto nível do StockSharp. Ela avalia as transições de cor do indicador personalizado DynamicRS_C para detectar suporte e resistência dinâmicos. Quando a linha fica magenta (índice de cor `0`) ela favorece setups altistas, e quando fica azul-violeta (índice de cor `2`) favorece setups baixistas. O port do StockSharp mantém o mesmo tempo de sinal, sinalizadores de permissão e estrutura de stop/take que o robô fonte.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A vela concluída selecionada por `SignalBar` muda a cor do indicador de qualquer coisa exceto `0` para `0`. A estratégia opcionalmente fecha uma posição vendida existente antes de entrar, replicando o portão `SellPosClose` original, e então abre uma comprada se `AllowBuyEntry` estiver habilitado.
  - **Vendido**: A vela avaliada muda a cor do indicador de qualquer coisa exceto `2` para `2`. A estratégia opcionalmente fecha uma comprada existente (`AllowBuyExit`) e então abre uma vendida se `AllowSellEntry` estiver habilitado.
- **Comprado/Vendido**: Negocia ambas as direções com interruptores independentes para entradas e saídas.
- **Critérios de saída**:
  - Posições compradas fecham quando um sinal vendido aparece e `AllowBuyExit` é verdadeiro, ou quando os limites de stop-loss / take-profit são atingidos.
  - Posições vendidas fecham quando um sinal comprado aparece e `AllowSellExit` é verdadeiro, ou quando os limites de risco são acionados.
- **Stops**: `StopLossPoints` e `TakeProfitPoints` são deslocamentos de preço absolutos do preço de entrada. Definir qualquer valor como zero desativa essa proteção.
- **Filtros**:
  - `SignalBar` determina quantas velas completamente fechadas atrás são inspecionadas para uma mudança de cor, imitando a busca do buffer original (`CopyBuffer(..., SignalBar, 2)`).
  - `CandleType` seleciona o período usado tanto para o indicador quanto para a lógica de negociação (padrão: velas de 4 horas, correspondendo ao EA).

## Parâmetros

- `CandleType` – Série de velas processada pela estratégia.
- `Length` – Profundidade de retrovisão usada pelo indicador DynamicRS_C para comparar máximas/mínimas (`Length` em MQL).
- `SignalBar` – Número de velas completamente fechadas para trás usadas para avaliação de sinais (equivalente à entrada do EA `SignalBar`).
- `AllowBuyEntry` / `AllowSellEntry` – Permite abrir posições compradas/vendidas em seus respectivos sinais.
- `AllowBuyExit` / `AllowSellExit` – Permite fechar posições compradas/vendidas existentes quando o sinal oposto aparece.
- `StopLossPoints` – Distância de perda absoluta do preço de entrada. Quando positivo fecha compradas abaixo e vendidas acima da entrada.
- `TakeProfitPoints` – Distância de lucro absoluta do preço de entrada. Quando positivo fecha compradas acima e vendidas abaixo da entrada.
- `Volume` – Tamanho de ordem base herdado de `Strategy.Volume`. Quantidade adicional é automaticamente adicionada para nivelar posições opostas quando o sinal solicita uma reversão.

## Lógica do indicador

O `DynamicRsCIndicator` incluído reproduz o comportamento do buffer de cor do script MetaTrader:

- Ele rastreia as últimas máximas e mínimas sobre a janela `Length` configurada e a barra imediatamente anterior.
- Quando uma máxima local é menor que a máxima anterior e a máxima há `Length` barras, e também está abaixo do valor anterior do indicador, o buffer muda para a cor `0` (magenta) e o valor salta para essa máxima.
- Quando uma mínima local é maior que a mínima anterior e a mínima há `Length` barras, e está acima do valor anterior do indicador, o buffer muda para a cor `2` (azul-violeta) e o valor salta para essa mínima.
- Caso contrário, o indicador mantém seu valor anterior. A cor neutra `1` atua como ponte entre os estados de tendência exatamente como no algoritmo original.

Ao vincular este indicador através de `BindEx`, a estratégia recebe tanto o valor numérico quanto o índice de cor discreto, garantindo que a avaliação de sinais e o tempo de negociação correspondam ao comportamento do especialista fonte.
