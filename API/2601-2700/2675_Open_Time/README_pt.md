# Estratégia de Horário de Abertura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia de Horário de Abertura é um sistema de trading com agendamento de tempo que replica o comportamento do consultor especializado do MetaTrader 5 *OpenTime*. A estratégia observa o relógio do mercado em velas fechadas e abre operações apenas dentro de uma janela de tempo configurável. Pode fechar qualquer posição ativa durante uma janela de saída dedicada, aplicar um trailing stop opcional e aplicar regras básicas de stop-loss e take-profit expressas em pips.

Ao contrário da versão de hedge original, este port do StockSharp funciona em uma carteira neteada: quando surge um sinal que conflita com a posição atual, a estratégia primeiro fecha a exposição oposta e depois abre a direção solicitada com o volume configurado.

## Fluxo de operações
1. **Janela de fechamento** – Se o indicador *Use Close Window* estiver ativado e o tempo atual cair dentro da janela de fechamento, a estratégia fecha imediatamente qualquer posição aberta. Nenhuma nova operação é permitida até que a janela termine.
2. **Atualização do trailing** – Quando o trailing está ativado e o mercado avançou pelo menos `TrailingStop + TrailingStep` pips a favor da posição atual, o trailing stop é aproximado ao preço pela distância definida em `TrailingStop`. Isso reproduz a lógica do MT5 onde o nível de stop é modificado somente após um passo mínimo.
3. **Verificações de risco** – Em cada vela fechada, a estratégia verifica se os limites de stop-loss ou take-profit foram atingidos. Se algum nível for tocado, a posição é fechada e todo o estado interno daquele lado é reiniciado.
4. **Janela de entrada** – Quando o tempo está dentro da janela de operações, a estratégia avalia os seletores de direção:
   - Se entradas compradas estão habilitadas e a posição neta atual é flat ou vendida, compra o volume configurado mais qualquer quantidade necessária para cobrir uma posição vendida existente.
   - Se entradas vendidas estão habilitadas e a posição neta é flat ou comprada, vende o volume configurado mais qualquer quantidade necessária para zerar uma posição comprada existente.

Cada entrada executada armazena o preço de entrada junto com os deslocamentos de stop e alvo (se diferentes de zero). Esses valores são reutilizados pela lógica de trailing e pelas verificações de saída subsequentes.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| Candle Type | Velas de 1 minuto | Tipo de dado usado para rastreamento de tempo; a estratégia reage apenas em velas fechadas. |
| Use Close Window | true | Habilita a janela de fechamento automático. |
| Close Hour / Close Minute | 20:50 | Início da janela de fechamento. A hora suporta valores 0–24; 24 passa para o dia seguinte. |
| Enable Trailing | false | Ativa a lógica de trailing stop. |
| Trailing Stop | 30 pips | Distância entre o preço e o trailing stop. Convertida em unidades de preço dependendo do tamanho do tick do instrumento. |
| Trailing Step | 3 pips | Movimento adicional necessário antes que o trailing stop avance novamente. |
| Trade Hour / Trade Minute | 18:50 | Hora de início da janela de trading que permite novas entradas. |
| Duration | 300 segundos | Duração compartilhada pelas janelas de abertura e fechamento. |
| Enable Sell / Enable Buy | Sell = true, Buy = false | Seleciona quais direções são permitidas. |
| Volume | 0.1 | Volume da ordem enviada com novas entradas. Ao reverter, volume extra é adicionado para zerar a exposição oposta. |
| Stop Loss | 0 pips | Distância de stop-loss inicial. Um valor zero desativa o stop estático e deixa o controle de saída para o trailing ou a janela de fechamento. |
| Take Profit | 0 pips | Distância de take-profit inicial. Um valor zero desativa o alvo de lucro. |

## Detalhes de implementação
- Os valores em pips são recalculados a partir de `Security.PriceStep`. Para símbolos cotados com três ou cinco decimais, o passo é multiplicado por dez para reproduzir a conversão de "pip" original do MT5.
- Tanto o trailing quanto os níveis de risco estáticos operam sobre os extremos da vela (`HighPrice`/`LowPrice`) para aproximar o comportamento tick a tick enquanto trabalha na API de alto nível baseada em velas.
- A estratégia reinicia o estado interno após cada saída para evitar reutilizar stops ou alvos desatualizados na próxima operação.
- Como o StockSharp trabalha com posições netas por padrão, posições compradas e vendidas simultâneas não são suportadas. A lógica de reversão imita o hedge do MT5 compensando a exposição existente antes de abrir o lado solicitado.

## Notas de uso
- Escolha um tipo de vela que corresponda à granularidade temporal exigida pela janela de trading. Um período mais curto (p. ex., 1 minuto) oferece temporização mais precisa.
- As janelas de fechamento e abertura compartilham o mesmo parâmetro de duração. Para desativar qualquer janela, defina a duração como zero ou desative *Use Close Window*.
- Os trailing stops se ativam apenas quando o mercado avançou pelo menos `Trailing Stop + Trailing Step` pips a partir do preço de entrada registrado, reproduzindo o comportamento original do passo de trailing.
