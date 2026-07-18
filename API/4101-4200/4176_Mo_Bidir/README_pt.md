# Estratégia de hedge MO Bidir
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Hedge Bidir MO** é uma versão StockSharp do MetaTrader 4 consultor especialista `mo_bidir_v0_1`. O robô original foi projetado para o gráfico de cinco minutos e sempre manteve uma exposição de mercado protegida: cada nova barra abria uma posição longa e curta com distâncias de stop-loss e take-profit predefinidas. A versão StockSharp reproduz esse comportamento usando velas finalizadas, auxiliares de pedidos de alto nível e parâmetros de risco explícitos medidos em pontos de instrumento.

## Lógica de negociação
1. Assine o tipo de vela configurado (período de cinco minutos por padrão) e processe apenas velas concluídas.
2. Assim que uma vela se fechar, inspecione as pernas internas da sebe. Se alguma perna permanecer aberta, a estratégia aguarda o acionamento de ordens de proteção e não abre posições adicionais.
3. Quando nenhuma perna estiver ativa, envie uma ordem de **compra no mercado** e uma ordem de **venda no mercado** de tamanho igual. Cada ordem executada torna-se uma perna de hedge independente monitorada pela estratégia.
4. Após o preenchimento de cada entrada, os limites de stop-loss e take-profit são calculados multiplicando as distâncias dos pontos configuradas pela etapa de preço do instrumento (ou incremento mínimo de preço quando a etapa não está disponível).
5. Em cada vela finalizada subsequente, a estratégia verifica os máximos e mínimos das velas:
   - As pernas longas fecham através de uma venda no mercado quando a mínima ultrapassa o nível de stop; se não for interrompido, uma alta que atinge a meta fecha a perna com lucro.
   - As pernas curtas fecham por meio de uma compra de mercado quando a máxima atinge o stop; caso contrário, uma baixa que atinge a meta gera o lucro.
   - Quando ambos os limites caem dentro da mesma vela, o stop loss é priorizado porque seu toque teria fechado a posição primeiro na implementação MetaTrader.
6. Assim que todas as pernas estiverem fechadas em seus níveis de proteção, a estratégia prepara imediatamente o próximo par coberto no fechamento da vela seguinte.

Este fluxo de trabalho mantém a paridade com a lógica MT4 enquanto depende exclusivamente de APIs StockSharp de alto nível (`BuyMarket`/`SellMarket`) e do processamento baseado em velas exigido pelas diretrizes de conversão.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `TradeVolume` | Tamanho do pedido aplicado a ambos os lados da cobertura. Deve ser positivo. |
| `StopLossPoints` | Distância do preço de entrada até o stop de proteção medido em pontos do instrumento. Use `0` para desativar a parada. |
| `TakeProfitPoints` | Distância alvo do preço de entrada em pontos do instrumento. Use `0` para desativar a meta de lucro. |
| `CandleType` | Prazo usado para detectar novas barras. O padrão é um período de cinco minutos. |

Todas as distâncias baseadas em pontos são convertidas em preços absolutos multiplicando o valor configurado pelo instrumento `PriceStep`. Se a etapa for indefinida, será utilizado o incremento mínimo de preço; quando nenhum valor está disponível, os níveis de proteção permanecem inativos.

## Gestão de risco
- Ambos os lados da cobertura utilizam o mesmo volume fixo e dependem de ordens de proteção simétricas.
- As distâncias de stop-loss e take-profit refletem os padrões MetaTrader (80 e 750 pontos respectivamente), preservando a relação "8 pips vs. 75 pips" em um símbolo forex de 5 dígitos.
- Cada etapa é fechada com uma ordem de mercado, liberando margem instantaneamente e permitindo que a etapa restante continue sem gerenciamento até que seu próprio nível de proteção seja atingido.

## Notas de implementação
- A estratégia processa estritamente **velas acabadas** para cumprir as regras de conversão de todo o projeto. O stop intrabarra ou os toques no alvo são inferidos a partir dos extremos da vela, portanto, os backtests sem dados de tick assumirão o stop acionado antes do alvo quando ambos os preços apareceram dentro da mesma barra.
- O livro-razão de hedge interno acompanha os preenchimentos independentemente da posição líquida do portfólio. Isso reflete o comportamento MetaTrader onde posições longas e curtas coexistem simultaneamente.
- Nenhuma lógica de rastreamento automatizada, filtros de sessão ou indicadores adicionais são introduzidos — a versão StockSharp permanece intencionalmente tão minimalista quanto o consultor especialista original.

## Dicas de uso
- Ajuste `TradeVolume` para corresponder aos tamanhos dos contratos do corretor e garantir que o instrumento suporte cobertura simultânea de compra/venda se o ambiente exigir.
- Se você precisar de valores baseados em pip (por exemplo, 8 pips), multiplique pelo número de pontos que representam um pip para o símbolo atual antes de atribuir o parâmetro.
- Combine a estratégia com StockSharp módulos de risco ou `StartProtection` se forem necessárias salvaguardas extras no nível do portfólio.
