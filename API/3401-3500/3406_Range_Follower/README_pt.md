# Estratégia de Seguidor de Alcance
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Range Follower reproduz o MetaTrader 5 consultor especialista "Range Follower" usando o StockSharp API de alto nível. Ele monitora a faixa de preço do dia atual em relação a um benchmark diário Average True Range (ATR) e abre uma única negociação de breakout quando o preço se afasta o suficiente da máxima ou mínima da sessão. A conversão mantém a abordagem original de dividir o ATR em uma parte de gatilho e uma parte residual que se torna a distância de lucro.

## Lógica de negociação
1. **Linha de base de volatilidade diária**
   - Um ATR de 20 períodos calculado em velas diárias fornece o intervalo de referência para o dia de negociação atual.
   - O valor ATR é dividido por `TriggerPercent` em dois segmentos: a distância de acionamento que deve ser excedida antes de entrar e a distância restante que é usada como meta de lucro.
2. **Rastreamento de alcance**
   - A estratégia registra continuamente a máxima e a mínima da sessão atual a partir da vela diária ativa.
   - As atualizações de nível 1 fornecem os melhores preços de compra e venda mais recentes que são usados para medir a distância das cotações atuais até os extremos da sessão.
3. **Entrada única por dia**
   - Quando a melhor oferta é superior à distância de disparo acima da mínima da sessão e nenhuma negociação foi aberta ainda, a estratégia compra no mercado.
   - Quando a melhor venda é superior à distância de disparo abaixo da máxima da sessão e nenhuma negociação foi aberta ainda, a estratégia vende a mercado.
   - Apenas uma negociação é permitida por dia; o sinalizador é redefinido quando uma nova sessão é iniciada.
4. **Stop-loss e take-profit**
   - Para posições longas, o stop loss é colocado uma distância de disparo abaixo do preço de entrada e o take-profit uma distância residual acima dele.
   - Para posições curtas, o stop loss está uma distância de gatilho acima do preço de entrada e o take-profit uma distância residual abaixo dele.
   - O monitoramento de preços é realizado tanto nos ticks de Nível 1 quanto nas atualizações de velas para fechar posições assim que um nível é violado.
5. **Reinicialização da sessão diária**
   - Na primeira vela de um novo dia de negociação, a estratégia fecha qualquer posição aberta, limpa o estado interno e recarrega a linha de base ATR.
   - Se o intervalo diário atual já exceder a distância de acionamento quando a sessão for inicializada, a negociação será ignorada pelo resto do dia para imitar a verificação de segurança do EA original.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | Velas de 15 minutos | Período de trabalho usado para detectar limites de sessão. |
| `TriggerPercent` | 60 | Porcentagem do ATR diário usado como distância de acionamento do rompimento. Deve ficar entre 10 e 90. |
| `Volume` | 0,1 | Volume de ordens de mercado para entradas longas e curtas. |

## Gestão de risco
- Stops e metas são derivados da mesma linha de base ATR para que a relação recompensa-risco seja sempre igual a `(100 - TriggerPercent) : TriggerPercent`.
- A estratégia registra uma única posição por vez e a liquida imediatamente quando o stop ou alvo é tocado, evitando múltiplas negociações sobrepostas.
- `StartProtection()` ativa a infraestrutura de proteção de StockSharp, permitindo que componentes externos anexem trailing stops ou proteções de portfólio, se necessário.

## Notas de implementação
- Os valores diários ATR são produzidos por uma assinatura de vela diária dedicada e o indicador `AverageTrueRange` vinculado ao API de alto nível.
- Os dados de nível 1 são necessários para espelhar as decisões baseadas em ticks do EA; os melhores preços de oferta e de venda impulsionam as verificações de entrada e saída.
- Os limites da sessão diária são derivados das velas do período de trabalho, garantindo que qualquer calendário de negociação usado em StockSharp redefinirá a estratégia de forma consistente.
- A conversão evita buffers de indicadores manuais ou loops históricos, contando, em vez disso, com campos com estado atualizados pelos retornos de chamada `Bind`.
