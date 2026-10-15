# Estratégia de número de rejeição
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de número de rejeição** é uma porta StockSharp do indicador MetaTrader `BounceNumber_V0.mq4` / `BounceNumber_V1.mq4`. A ferramenta original era um analisador visual que contava quantas vezes o preço tocou um canal simétrico antes de sair dele. Essa estratégia C# recria o contador de rejeição com o API de alto nível, armazena os resultados em uma tabela de distribuição e relata cada ciclo concluído por meio do log de estratégia. A implementação permanece fiel à lógica MetaTrader enquanto a adapta ao pipeline orientado a eventos de StockSharp.

Ao contrário do indicador original, o porto funciona como um componente estratégico. Ele assina velas finalizadas, monitora toques de banda e rastreia quantos golpes alternados ocorrem antes que o preço saia do canal duas vezes sua meia largura. As estatísticas coletadas podem ser consumidas da propriedade `BounceDistribution` ou das mensagens de log geradas.

## Como funciona
1. Quando a estratégia é iniciada, ela valida que o instrumento expõe um `PriceStep` diferente de zero. As entradas baseadas em pontos dependem deste valor para converter MetaTrader "pontos" em distâncias de preços decimais.
2. Uma assinatura de vela criada a partir de `CandleType` alimenta o analisador de rejeição apenas com barras completas.
3. A primeira vela recebida define o centro do canal (seu preço de fechamento). Uma banda simétrica cuja meia largura é igual a `ChannelPoints * PriceStep` é criada em torno desse centro.
4. Cada nova vela finalizada incrementa o contador de ciclos e é avaliada com três regras:
   - **Detecção de breakout**: se o alcance da vela ultrapassar `center ± 2 * halfWidth`, o ciclo atual termina e sua contagem de saltos é registrada.
   - **Toque na banda inferior**: se a vela abrange a banda inferior e o toque anterior também não foi um toque na banda inferior, o contador de salto aumenta em um e a direção muda para "inferior".
   - **Toque na faixa superior**: regra simétrica para a faixa superior.
5. Se um ciclo durar mais velas que `MaxHistoryCandles` (e o parâmetro for positivo), o canal será redefinido à força, garantindo que o histograma seja atualizado mesmo quando o preço oscilar lateralmente para sempre.
6. A cada reinicialização do ciclo o dicionário de distribuição é atualizado e um log de informações é produzido, espelhando o comportamento dos contadores da interface original.

A estratégia não faz pedidos intencionalmente. Deve ser hospedado junto com outros componentes (dashboards, UI, exportadores de dados) que consomem as estatísticas `BounceDistribution`.

## Parâmetros
| Nome | Tipo | Padrão | MetaTrader analógico | Descrição |
| --- | --- | --- | --- | --- |
| `MaxHistoryCandles` | `int` | `10000` | entrada `maxbar` | Número máximo de velas permitidas dentro de um ciclo antes de um reset forçado. Defina como `0` para desativar a redefinição de segurança. |
| `ChannelPoints` | `int` | `300` | entrada `BPoints` | Meia largura do canal de rejeição expressa em faixas de preço (`PriceStep` múltiplos). |
| `CandleType` | `DataType` | `M1` prazo | entrada `TF` | Série de velas usada para cálculos de rejeição. |

## Diferenças vs. código MetaTrader
- O histograma é armazenado como um dicionário em vez de objetos de texto no gráfico. Isso torna as informações mais fáceis de exportar ou visualizar em painéis StockSharp.
- As entradas específicas da UI do indicador (cores, fontes, botões) são removidas porque eram cosméticas e não têm impacto na lógica analítica.
- A redefinição forçada por `MaxHistoryCandles` agora é opcional (`0` a desativa) e funciona em fluxos de dados ao vivo, enquanto MetaTrader processou um bloco histórico finito.
- Todas as mensagens informativas são escritas em inglês por meio de `AddInfoLog`, atendendo ao requisito de comentários/logs de código somente em inglês.

## Dicas de uso
- Certifique-se de que a segurança selecionada defina `PriceStep`; caso contrário, a estratégia lançará uma exceção no início porque os deslocamentos baseados em pontos não podem ser calculados.
- Combine a estratégia com widgets de UI personalizados ou scripts que leem `BounceDistribution` para replicar a grade de contagens MetaTrader.
- Use valores menores para `ChannelPoints` ao analisar ruído intradiário e valores maiores para prazos mais altos ou instrumentos voláteis.
- Para emular a verificação histórica da versão MQL, inicie a estratégia com `HistoryBuildMode` ativado em seu conector e deixe-o processar o intervalo histórico solicitado; a distribuição será preenchida assim que as velas preenchidas forem entregues.
