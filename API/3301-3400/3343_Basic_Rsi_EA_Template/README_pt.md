# Estratégia de modelo básica RSI EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de modelo básica RSI EA** replica o MetaTrader 4 consultor especialista "Basic Rsi EA Template.mq4" (MQL/26750). Ele observa o Índice de Força Relativa (RSI) na série de velas selecionada e reage quando o impulso se estende para zonas de sobrecompra ou sobrevenda configuráveis. A conversão StockSharp mantém o fluxo de trabalho simples de uma posição e a lógica protetora de parada/tomada do robô original enquanto adota a assinatura de alto nível API.

## Lógica da estratégia

### Indicadores
- **Índice de Força Relativa (RSI)** com período configurável calculado no tipo de vela escolhido.

### Condições de Entrada
- **Configuração longa**: quando RSI cai abaixo de `OversoldLevel` e a estratégia não tem posição aberta, ela envia uma ordem de compra a mercado para o `OrderVolume` configurado.
- **Configuração curta**: quando RSI ultrapassa `OverboughtLevel` e a estratégia não tem posição aberta, ela envia uma ordem de venda a mercado para o `OrderVolume` configurado.

O algoritmo funciona em modo de compensação: apenas uma posição pode existir a qualquer momento. Se uma posição longa estiver ativa, a estratégia espera que ela feche antes de uma entrada curta (e vice-versa).

### Condições de saída
- **Parada protetora**: `StopLossPips` é convertido em uma distância de preço absoluta usando o tamanho do tick do instrumento. Assim que o preço atingir esse valor, o mecanismo de proteção integrado fecha a posição.
- **Take Profit**: `TakeProfitPips` é processado da mesma maneira – quando o preço se move a favor pela distância configurada, a posição é fechada com lucro.

Não há saída adicional baseada em sinal ou à direita. A estratégia baseia-se puramente nas distâncias de proteção ou na intervenção manual para sair das negociações, refletindo o design minimalista do modelo original.

### Tratamento de riscos e volumes
- `OrderVolume` define o valor fixo enviado com cada ordem de mercado (padrão 0,01 lote, correspondendo à amostra MQL).
- A estratégia não faz pirâmide nem hedge. Se um stop protetor ou take-profit fechar a negociação ativa, o algoritmo fica plano e aguarda o próximo gatilho RSI.

## Parâmetros
- `CandleType`: série de velas usada para geração de sinal (padrão: período de 1 minuto).
- `RsiPeriod`: número de barras na janela RSI (padrão: 14).
- `OverboughtLevel`: limite RSI que permite entradas curtas (padrão: 70).
- `OversoldLevel`: limite RSI que permite entradas longas (padrão: 30).
- `StopLossPips`: distância de parada em pips convertida em unidades de preço absoluto (padrão: 30 pips).
- `TakeProfitPips`: meta de lucro em pips convertida em unidades de preço absoluto (padrão: 20 pips).
- `OrderVolume`: volume fixo para ordens de mercado (padrão: 0,01).

## Notas de implementação
- Usa `SubscribeCandles(...).Bind(rsi, ProcessCandle)` para que os valores do indicador fluam diretamente para o método de processamento sem gerenciamento manual de buffer.
- `CreateProtectionUnit` recria o tratamento de pip MQL: instrumentos com 3 ou 5 casas decimais usam um multiplicador de 10x para mapear pips para etapas de preço.
- Todas as verificações dos indicadores são executadas em velas finalizadas para evitar múltiplas ordens na mesma barra.
- A conversão pressupõe uma conta de compensação, ao contrário do modo de cobertura de MetaTrader. Consequentemente, as negociações opostas fecham a posição atual em vez de criar vários tickets.
- Os comentários e registros embutidos estão em inglês para ajudar na manutenção futura.

## Dicas de uso
- Ajuste `CandleType` ao instrumento e período que você deseja negociar (por exemplo, mude para velas horárias para configurações de swing).
- Ajuste `StopLossPips` e `TakeProfitPips` para que correspondam à volatilidade do instrumento; as distâncias de proteção são essenciais para o controle de riscos.
- Combine a estratégia com portfólio StockSharp ou módulos de risco se precisar de gerenciamento financeiro avançado além da lógica do modelo.
