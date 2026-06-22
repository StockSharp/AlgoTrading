# Estratégia SilverTrend V3 (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia SilverTrend V3 é um sistema seguidor de momentum que se origina do consultor especialista do MetaTrader 5 "SilverTrend v3". O port para o StockSharp reproduz a lógica original adaptando-a à API de estratégias de alto nível. A ideia central é detectar momentum de alta ou baixa usando o cálculo de canal SilverTrend, confirmá-lo com o oscilador de perfil de mercado J_TPO e gerenciar as posições resultantes com stops protetores, lógica de trailing e um filtro de sessão de sexta-feira.

## Motor de sinais

1. **Direção SilverTrend**
   - Usa uma janela deslizante de 350 barras com um parâmetro de suavização de 9 barras para calcular suporte dinâmico (`smin`) e resistência (`smax`).
   - Quando o fechamento atual cai abaixo de `smin`, o sistema sinaliza um regime de baixa; um fechamento acima de `smax` muda o regime para alta.
   - O cálculo itera da barra mais antiga para a mais recente para replicar a natureza recursiva do código MQL original.

2. **Confirmação J_TPO**
   - Implementa o oscilador J_TPO original de 14 períodos que mede como os preços se agrupam dentro de uma distribuição de curto prazo.
   - Permite apenas entradas compradas quando o oscilador é positivo e entradas vendidas quando é negativo, filtrando mudanças de momentum fracas.

3. **Detecção de mudança de sinal**
   - Uma operação é iniciada apenas quando a direção SilverTrend recém-calculada difere do valor anterior, garantindo que a estratégia reaja a mudanças genuínas de regime em vez de ruído.

## Gestão de operações

- **Entradas a mercado** – A estratégia opera o `Volume` configurado. Se uma posição contrária estiver aberta, ela é fechada e revertida em uma única ordem a mercado.
- **Stop loss inicial** – Opcional. Definido em passos de preço relativos ao preço de entrada (convertido com o `PriceStep` do instrumento).
- **Take profit** – Opcional. Também definido em passos de preço e avaliado contra extremos da vela para imitar o comportamento original de modificação de ordens.
- **Trailing stop** – Ativa-se quando o preço se move favoravelmente pela distância de trailing configurada. Para posições compradas, o stop sobe gradualmente; para vendidas, desce, correspondendo à lógica do MetaTrader.
- **Saída por sinal oposto** – Quando o regime anterior aponta na direção oposta, qualquer posição existente é liquidada no próximo fechamento de vela.
- **Bloqueio de operações na sexta-feira** – Novas posições são ignoradas após a hora especificada nas sextas-feiras para evitar gaps de fim de semana, exatamente como no EA fonte.

## Parâmetros

| Nome | Valores padrão | Descrição |
| --- | --- | --- |
| `TrailingStopPoints` | 50 | Distância do trailing stop medida em passos de preço. Definir como zero para desabilitar o trailing. |
| `TakeProfitPoints` | 50 | Distância do take profit em passos de preço. Zero desabilita o alvo. |
| `InitialStopLossPoints` | 0 | Stop protetor inicial em passos de preço. Zero deixa a posição sem stop inicial. |
| `FridayCutoffHour` | 16 | Hora da bolsa após a qual não são permitidas novas entradas na sexta-feira. Usar `0` para permitir operações o dia todo. |
| `CandleType` | Velas de 1 hora | Série de dados que alimenta os indicadores. Qualquer período suportado pode ser usado. |
| `Volume` | 1 lote | Tamanho de operação para cada posição (propriedade `Volume` do StockSharp). |

Todas as distâncias são multiplicadas por `PriceStep` em tempo de execução, o que adapta automaticamente a estratégia ao tamanho do tick do instrumento (incluindo símbolos forex de 3/5 dígitos).

## Requisitos de dados e ambiente

- Requer pelo menos 360 velas completas antes de produzir sinais ao vivo para que os buffers de SilverTrend e J_TPO estejam completamente formados.
- Projetado para operação com instrumento único via `SubscribeCandles`. O override `GetWorkingSecurities` garante que a estratégia se inscreva apenas no instrumento e período configurados.
- Usa `StartProtection()` para habilitar o serviço padrão de proteção de posições do StockSharp uma vez na inicialização.

## Notas de uso

- O algoritmo espera instrumentos em tendência, como principais pares forex ou futuros líquidos; adaptar o período à volatilidade do mercado.
- Como o cálculo do SilverTrend é recursivo, reiniciar a estratégia com velas históricas insuficientes atrasará a formação de sinais até que dados suficientes sejam coletados.
- A implementação da API de alto nível usa extremos de velas para simular o gerenciamento de ordens (stop loss, take profit, trailing). Em operações ao vivo, considere combinar a lógica com ordens stop/limite reais se sua infraestrutura exigir.
- O port armazena o estado interno (`_previousSignal`, `_entryPrice`, trailing stops) exatamente uma vez por vela finalizada, correspondendo ao comportamento "uma operação por barra" do EA original.

## Detalhes da conversão

- Reproduz fielmente as rotinas matemáticas de `SilverTrend v3.mq5`, incluindo o algoritmo J_TPO de arrays aninhados.
- Aplica boas práticas de C#: os parâmetros são expostos via `StrategyParam<T>`, todos os comentários estão em inglês, e a indentação usa tabulações conforme as diretrizes do repositório.
- Nenhuma versão em Python está incluída nesta versão, conforme os requisitos da tarefa.
