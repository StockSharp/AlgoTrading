# Estratégia eInTradePanel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia eInTradePanel automatiza o fluxo de trabalho do painel de negociação original do MetaTrader. Ela permite os mesmos oito modos de ordem (mercado, stop, limite e stop-limite em ambas as direções) enquanto calcula automaticamente distâncias de disparo, entrada, stop-loss e take-profit a partir do spread atual e uma estimativa de ATR sensível à volatilidade. Ordens de proteção são simuladas através do monitoramento de velas para que a estratégia possa ser usada com provedores de dados que não suportam ordens SL/TP anexadas.

## Destaques

- **Modos de ordem** – escolher entre Buy, Sell, Buy/Sell Stop, Buy/Sell Limit ou Buy/Sell Stop-Limit. Ordens stop-limite são armadas assim que o preço alcança a distância de disparo e então submetem a entrada limite.
- **Distâncias dinâmicas** – níveis pendentes, disparadores, stops e alvos são proporcionais ao maior entre o spread atual ou um spread sintético derivado do ATR (`ATR × AtrFactor`). Quando o ATR não está pronto, uma distância de tick base configurável é usada.
- **Adaptação à volatilidade** – o comprimento do ATR segue o painel original (55) para que os offsets reajam a mudanças de regime sem ajuste extra.
- **Expiração de ordens** – janela de cancelamento opcional com imposição de tempo mínimo de vida (padrão 11 minutos) mantém ordens pendentes obsoletas fora do livro.
- **Gestão de risco** – cada posição aberta é monitorada em cada vela fechada; se o máximo/mínimo perfura o stop ou alvo calculado, a posição é fechada a mercado.
- **Consciência de cotação** – a estratégia se inscreve no livro de ordens para obter os melhores preços de oferta/demanda para cálculos de offset mais precisos, recorrendo a fechamentos de velas quando a profundidade não está disponível.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `Volume` | Tamanho de ordem usado para todas as entradas. |
| `Mode` | Modo de entrada (mercado, stop, limite ou stop-limite). |
| `Candle Type` | Agregação usada para ATR e verificações de execução baseadas em velas. |
| `Base Ticks` | Distância mínima de tick quando os dados de ATR não estão disponíveis. |
| `Pending Multiplier` | Multiplicador aplicado à distância de tick base para offsets de ordens pendentes. |
| `Trigger Multiplier` | Multiplicador adicional para distâncias de disparo stop-limite. |
| `Stop Multiplier` | Multiplicador para distância de stop-loss (definir como 0 para desabilitar). |
| `Take Multiplier` | Multiplicador para distância de take-profit (definir como 0 para desabilitar). |
| `Use ATR` | Habilita o escalonamento baseado em ATR de todas as distâncias. |
| `ATR Factor` | Fração do ATR tratada como spread sintético ao escalonar. |
| `Expiration` | Minutos até que as ordens pendentes sejam canceladas (0 as mantém GTC). |
| `Min Expiration` | Tempo de vida mínimo pendente em minutos, replicando a proteção do painel. |

## Lógica de negociação

1. **Preparação de dados** – a estratégia se inscreve no tipo de vela configurado e mantém um ATR de 55 períodos atualizado. Instantâneos do livro de ordens atualizam o último preço de oferta/demanda visto.
2. **Cálculo de distâncias** – cada vela finalizada recalcula a distância de tick base a partir do ATR e spread, então deriva preços pendentes, de disparo, stop e take-profit de acordo com o modo selecionado.
3. **Submissão de ordens** –
   - Os modos de mercado executam imediatamente na próxima vela finalizada enquanto a estratégia está plana.
   - Os modos stop e limite colocam a ordem pendente correspondente e opcionalmente a cancelam após a janela de expiração.
   - Os modos stop-limite aguardam até que o preço de disparo seja impresso pelo máximo/mínimo da vela, então submetem a entrada limite.
4. **Supervisão de posição** – uma vez que uma posição está aberta, a estratégia verifica as velas completadas para violações de stop ou alvo e fecha a posição a mercado se qualquer nível for violado.
5. **Reinicialização de estado** – quando a estratégia está plana e nenhuma ordem está ativa, os níveis são recalculados para que uma nova operação possa ser preparada na próxima vela.

A abordagem reflete o painel manual enquanto permanece compatível com a API de alto nível do StockSharp e o fluxo de ordens assíncrono.
