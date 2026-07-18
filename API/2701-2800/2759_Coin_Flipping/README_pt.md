# Estratégia de Cara ou Coroa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia de Cara ou Coroa é um port literal do clássico consultor especialista do MetaTrader que decide se comprar ou vender simulando um lançamento de moeda. Cada vela concluída desencadeia uma nova decisão quando a estratégia está sem posição, de modo que o sistema alterna por uma série contínua de negociações independentes. A conversão do StockSharp mantém o comportamento intencionalmente simples: apenas uma posição é mantida por vez e cada negociação é combinada com um take-profit e stop-loss simétricos expressos em pips.

Embora a ideia central seja intencionalmente ingênua, o exemplo demonstra como traduzir até mesmo Consultores Especialistas muito pequenos para a API de alto nível do StockSharp. A estratégia é útil como auxílio didático para configurar assinaturas, auxiliares de gestão de dinheiro e ordens protetoras.

## Lógica de trading
1. Ao iniciar a estratégia, o gerador de números aleatórios é semeado com a contagem de ticks do ambiente atual, correspondendo ao espírito da chamada original `MathSrand(GetTickCount())` do MQL.
2. Para cada vela finalizada (o período padrão é 1 minuto, mas qualquer tipo de vela pode ser fornecido), a estratégia verifica se o trading é permitido e se não há posição aberta no momento.
3. Quando sem posição, o gerador produz 0 ou 1. Um valor de 0 resulta em uma ordem de compra a mercado, enquanto 1 aciona uma ordem de venda a mercado. O volume é calculado dinamicamente com base na porcentagem de risco configurada e na distância do stop-loss.
4. As ordens protetoras criadas por `StartProtection` anexam um stop-loss e take-profit a cada posição para que o gerenciamento de saída permaneça automático.

Nenhum outro filtro é usado: toda vez que uma posição é fechada, a próxima vela cria imediatamente uma nova negociação.

## Dimensionamento de posição
A versão do StockSharp reinterpreta a fórmula do tamanho do lote para trabalhar com valores de portfólio. O valor de risco é calculado como `Portfolio.CurrentValue * RiskPercent / 100`. Este capital é dividido pela distância do stop-loss em unidades de preço (pips convertidos usando o passo de preço do instrumento) para derivar o número de contratos. O auxiliar então arredonda o tamanho para o passo de volume admissível mais próximo e aplica limites da bolsa através de `MinVolume` e `MaxVolume`.

Isso mantém o espírito do código original — arriscar uma porcentagem fixa do patrimônio por negociação — garantindo que o tamanho da ordem respeite os metadados de segurança do StockSharp.

## Parâmetros
| Parâmetro | Descrição | Padrão | Notas |
| --- | --- | --- | --- |
| `RiskPercent` | Porcentagem do portfólio arriscada em cada negociação. | `2` | Aumentar este número amplifica o volume; reduções tornam as ordens menores. |
| `TakeProfitPips` | Distância entre a entrada e o nível de take-profit em pips. | `20` | Convertido para preço absoluto usando o passo de preço do instrumento e passado para `StartProtection`. |
| `StopLossPips` | Distância entre a entrada e o nível de stop-loss em pips. | `10` | Também convertido em unidades de preço; o mesmo valor é usado para o dimensionamento de posição. |
| `CandleType` | Assinatura de velas que programa o loop de decisão. | `período de 1 minuto` | Qualquer tipo de vela do StockSharp pode ser fornecido; intervalos maiores desaceleram o ritmo de trading. |

## Gestão de risco
`StartProtection` é iniciado uma vez durante `OnStarted` com as distâncias de take-profit e stop-loss calculadas. O StockSharp então gerencia as ordens protetoras automaticamente, espelhando os argumentos `OrderSend` no script MQL. Como a estratégia só opera quando `Position == 0`, não é necessário cancelar ou reenviar manualmente as ordens existentes; a plataforma cancela as ordens protetoras assim que a posição é fechada.

## Notas de implementação
- O processamento de velas usa o padrão de alto nível `SubscribeCandles().Bind(...)` para clareza e simplicidade.
- As declarações de log descrevem a direção escolhida e o volume para que os backtests mostrem claramente como o gerador pseudoaleatório se comporta.
- A normalização do volume leva em conta `VolumeStep`, `MinVolume` e `MaxVolume`, garantindo que os tamanhos gerados cumpram a especificação do instrumento.
- O código mantém todos os comentários em inglês, conforme necessário, e espelha a estrutura exigida pelas diretrizes do repositório.

## Notas de uso
- Como a direção de trading é aleatória, não se espera lucratividade a longo prazo. Usar a estratégia para fins de demonstração ou teste.
- Garantir que o portfólio atribuído à estratégia tenha um `CurrentValue` positivo; caso contrário, o cálculo de risco retorna zero e nenhuma negociação será feita.
- Ajustar o tipo de vela se preferir que o lançamento de moeda ocorra com menos frequência (por exemplo, velas horárias) ou com mais frequência (por exemplo, velas de tick).
- Ao otimizar, é possível explorar distâncias alternativas de take-profit e stop-loss ou reduzir a porcentagem de risco para manter as reduções gerenciáveis.
