# ADX Estratégia de tendências simples
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **ADX Simple Trend Strategy** é uma versão direta do clássico MetaTrader consultor especialista "ADX Simple". Ele segue a direção do Índice Direcional Médio (ADX) comparando os indicadores de movimento direcional positivo e negativo (DI+ e DI-) e exigindo que a linha principal ADX suba antes de abrir qualquer negociação. A versão StockSharp mantém a natureza minimalista do sistema original enquanto o adapta a padrões e controles de risco API de alto nível.

## Pilha de Indicadores
- **Índice direcional médio (ADX)** com período configurável (padrão 25).
  - Fornece a **linha ADX principal** usada para confirmar a força da tendência.
  - Fornece valores **DI+** e **DI-** que definem o domínio de alta ou baixa.
- **O período** pode ser selecionado por meio de `CandleType` (o padrão é velas de 15 minutos).

## Geração de Sinal
### Entrada longa
1. Aguarde uma vela finalizada e um valor ADX finalizado.
2. Confirme se DI+ está acima de DI- na mesma barra.
3. Exige que a linha principal ADX seja estritamente maior que seu valor anterior (a tendência é de fortalecimento).
4. Se não existir nenhuma posição aberta, envie uma ordem de compra a mercado usando o volume da estratégia.

### Entrada curta
1. Aguarde uma vela terminada e uma leitura finalizada de ADX.
2. Confirme se DI- está acima de DI+.
3. Exija que a linha principal ADX seja maior que seu valor anterior.
4. Se estiver estável, envie uma ordem de venda a mercado com o volume da estratégia.

### Sair da lógica
- **Fechar Longo**: Quando DI- cruza acima de DI+ (o impulso da tendência se torna de baixa).
- **Fechar Short**: Quando DI+ cruza acima de DI- (o impulso da tendência se torna de alta).
- A verificação de inclinação ADX não é necessária para saídas, espelhando o EA original que fechou posições imediatamente após um cruzamento DI.

## Gerenciamento de posição
- A estratégia é sempre plana, longa ou curta; nunca mantém posições simultâneas em ambas as direções.
- As ordens de mercado são dimensionadas usando a propriedade integrada `Strategy.Volume` (padrão 1). Ajuste esta propriedade ao configurar a instância da estratégia para corresponder ao tamanho do seu instrumento.
- Não há ordens automáticas de stop-loss ou take-profit. O risco deve ser controlado externamente ou modificando a estratégia.

## Parâmetros
| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|---------|-------------|
| `AdxPeriod` | `int` | 25 | Comprimento de lookback para cálculos ADX, DI+ e DI-. |
| `CandleType` | `DataType` | Período de 15 minutos | Assinatura de velas usada para conduzir cálculos de indicadores. |

## Diferenças da versão original MQL
- Gestão de dinheiro: os lotes originais EA redimensionados com base no saldo da conta; a estratégia StockSharp usa `Strategy.Volume` e deixa o gerenciamento de capital para o ambiente de hospedagem.
- Acompanhamento de pedidos: em vez de iterar por meio de pools de pedidos MetaTrader, StockSharp depende do valor `Position` integrado.
- Tratamento de dados: a estratégia ignora velas inacabadas e negocia apenas com dados finalizados.
- Os ganchos de registro e visualização estão disponíveis por meio dos auxiliares `CreateChartArea`, `DrawCandles` e `DrawIndicator` para facilitar a depuração.

## Diretrizes de uso
1. Anexe a estratégia a um instrumento com movimento de tendência suficiente (por exemplo, principais FX ou índices).
2. Defina o tipo de vela desejado e o comprimento ADX através de parâmetros antes de iniciar a estratégia.
3. Opcionalmente, ative o gerenciamento de risco em nível de portfólio (stop-outs, limites de saque) por meio do aplicativo de hospedagem.
4. Monitore os cruzamentos DI e a inclinação ADX no visualizador de gráfico para verificar o comportamento.

## Estendendo a Estratégia
- Adicione filtros de volatilidade (ATR, desvio padrão) para evitar condições de baixa volatilidade.
- Apresente a automação de stop-loss/take-profit chamando `StartProtection` ou lógica de pedido personalizada em `ProcessCandle`.
- Combine com filtros de prazo mais altos assinando fluxos de velas adicionais.

Esta documentação tem como objetivo fornecer uma visão abrangente da estratégia de tendência simples do ADX para que você possa implantá-la e estendê-la com segurança dentro da estrutura do StockSharp.
