# Estratégia ReInitChart
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o utilitário **ReInitChart** do MetaTrader para o StockSharp. O script original criava um botão em cada gráfico que trocava temporariamente o período de tempo para forçar o recálculo dos indicadores. A versão do StockSharp mantém o mesmo espírito expondo um interruptor de atualização manual e um temporizador automático opcional que reiniciam o indicador SMA interno e registram o evento de atualização. Uma regra simples de seguimento de tendência SMA é aplicada para demonstrar o trading após a reconstrução do indicador.

## Como funciona

1. **Feed de dados primário** – a estratégia subscreve ao período de tempo definido por `CandleType` e calcula uma média móvil simples com comprimento `SmaLength`.
2. **Atualização manual** – quando `ManualRefreshRequest` se torna `true`, o estado da média móvil é reiniciado, o indicador é limpo e a ação é reportada no log junto com os metadados do botão preservados (`RefreshCommandName`, `RefreshCommandText`, `TextColorName`, `BackgroundColorName`).
3. **Atualização automática** – habilitar `AutoRefreshEnabled` agenda reinicializações recorrentes a cada `AutoRefreshInterval`, reproduzindo a reinicialização orientada por temporizador do MetaTrader.
4. **Lógica de trading** – após o SMA ser formado, a estratégia mantém no máximo uma posição. Vai longa quando o preço de fechamento está acima do SMA e muda para curta quando o preço cai abaixo, fechando primeiro o lado oposto.

Este comportamento espelha a ideia de reinicializar todos os gráficos do Expert Advisor original enquanto usa componentes idiomáticos do StockSharp (reinicialização do indicador e registro) em vez de trocar os períodos de tempo do gráfico.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `CandleType` | Período de tempo de trabalho para subscrição de velas. |
| `SmaLength` | Número de velas usadas para a média móvil que é reconstruída após cada atualização. |
| `AutoRefreshEnabled` | Habilita o temporizador de atualização periódica. |
| `AutoRefreshInterval` | Intervalo entre eventos de atualização automática. |
| `ManualRefreshRequest` | Definir como `true` manualmente para acionar uma atualização imediata. A estratégia o limpa após o processamento. |
| `RefreshCommandName` | Metadados refletindo o nome do botão do MetaTrader; reportado nos logs quando ocorre uma atualização. |
| `RefreshCommandText` | Metadados refletindo o título do botão do MetaTrader; reportado nos logs quando ocorre uma atualização. |
| `TextColorName` | Descrição da cor do texto do botão preservada do script MQL. |
| `BackgroundColorName` | Descrição da cor de fundo do botão preservada do script MQL. |

## Uso

1. Configure `CandleType` e `SmaLength` para corresponder ao mercado e ao período de tempo que deseja monitorar.
2. Habilite `AutoRefreshEnabled` e escolha `AutoRefreshInterval` se precisar de reconstruções periódicas do indicador. Deixe-o desabilitado quando quiser apenas controle manual.
3. Alterne `ManualRefreshRequest` para `true` sempre que quiser limpar o estado do indicador. O indicador é automaticamente redefinido para `false` assim que a atualização é registrada.
4. Inicie a estratégia para subscrever dados de mercado. Ela desenha velas, a curva SMA e suas próprias operações no gráfico, e executa os trades básicos de seguimento de tendência SMA assim que o indicador estiver pronto.

## Diferenças em relação ao script MQL original

- O StockSharp não expõe botões de gráfico da mesma forma, portanto o gatilho de atualização é implementado através de parâmetros de estratégia.
- Em vez de saltar entre os períodos de tempo M1 e M5, o port do StockSharp reinicia seus indicadores diretamente, o que é mais confiável dentro do framework.
- Rótulos e cores dos botões são retidos como metadados para registro a fim de manter um vínculo com a interface do MetaTrader, mesmo que nenhum controle no gráfico seja criado.
