# Estratégia de proteção de riscos em camadas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de proteção de risco em camadas** é uma conversão direta do MetaTrader consultor especialista "RiskManager". O algoritmo rastreia continuamente a curva de patrimônio do portfólio e ajusta a exposição ao mercado usando o Commodity Channel Index (CCI), múltiplos do Average True Range (ATR) e um modelo de dimensionamento de posição em camadas. Quando as métricas de risco caem abaixo dos limites configuráveis, a estratégia muda automaticamente para o modo de hedge, fecha posições em eventos de lucro ou redução e pode, opcionalmente, estabilizar no ponto de equilíbrio.

## Lógica de negociação
- **Condições do indicador** – A estratégia assina a série de velas primárias (prazo configurável) e calcula:
  - CCI usando o período definido pelo usuário. As negociações longas exigem que o CCI caia abaixo do limite negativo e as negociações curtas exigem que ele suba acima do limite positivo.
  - ATR com um período fixo de 14 para derivar distâncias de take-profit e stop-loss ajustadas à volatilidade para cada camada aberta.
  - Uma média móvel dos volumes das velas. A negociação é habilitada somente quando a média móvel das últimas 50 velas concluídas excede o volume da vela anterior, replicando o filtro "Ativo" original.
- **Entradas em camadas** – A exposição máxima é distribuída por um número configurável de camadas. Cada novo pedido usa o volume por camada (`MaxVolume / Layers`). Entradas adicionais são bloqueadas quando o uso relativo da camada (`Orders / Layers * 100`) excede a integridade atual do sistema.
- **Gerenciamento de pedidos** – Cada camada aberta armazena seu preço de entrada junto com níveis de stop-loss e take-profit baseados em ATR. Em cada vela concluída, a faixa máxima/inferior é verificada para decidir se alguma camada deve ser fechada devido ao alcance de seus níveis de proteção.
- **Modo de hedge** – Quando `MultiPairTrading` está desativado e a porcentagem de saúde calculada cai abaixo de `HedgeLevel`, a estratégia registra a contagem de pedidos atual e começa a abrir camadas do lado oposto até que o requisito de taxa de hedge seja alcançado. A cobertura é desativada automaticamente quando a saúde se recupera acima do limite.
- **Controles de patrimônio** – Várias proteções refletem o consultor especialista original:
  - Stop hard equity definido por `RiskLimit` (capital inicial menos limite de risco).
  - Meta de lucro expressa como compensação aditiva sobre o capital inicial.
  - Nível de "equidade próxima" rolante que adiciona `CloseProfitBuffer` ao saldo atual cada vez que todas as posições são achatadas com sucesso.
  - Saída de equilíbrio opcional que fecha todas as negociações quando o patrimônio atinge o capital de equilíbrio armazenado.
  - Interruptor manual "Hard Close" que força imediatamente uma posição plana.

## Parâmetros
- `AllowLong` / `AllowShort` – Habilite entradas longas ou curtas respectivamente.
- `MaxVolume` – Volume total de posição alocado em todas as camadas.
- `Layers` – Número máximo de camadas que podem ser abertas simultaneamente.
- `CciLength` / `CciLevel` – Período e limite para o filtro CCI.
- `StopLossMultiple` / `TakeProfitMultiple` – multiplicador ATRes que definem níveis de proteção para cada camada.
- `CloseProfitBuffer` – Lucro adicionado ao saldo ao reciclar a meta móvel de fechamento de capital. Também usado no cálculo do capital de equilíbrio.
- `ManualCapital` – Substitui o capital inicial usado para todos os cálculos de risco (definido como zero para usar o saldo do portfólio ativo na inicialização).
- `RiskLimit` – Rebaixamento máximo tolerado do capital inicial.
- `ProfitTarget` – Meta de lucro aditivo que pausa a negociação quando alcançada.
- `MultiPairTrading` – Quando verdadeiro, a cobertura é desativada mesmo se a saúde cair abaixo do limite.
- `HedgeLevel` / `HedgeRatio` – Porcentagem de integridade que inicia o hedge e proporção de camadas adicionais necessárias durante o modo de hedge.
- `CloseAtBreakEven` – Habilita a lógica de saída do ponto de equilíbrio.
- `HardClose` – Força o nivelamento imediato e pausa as negociações adicionais enquanto for verdadeiro.
- `CandleType` – Série de velas usada para avaliação de indicadores e decisões comerciais.

## Notas
- A estratégia pressupõe o preenchimento imediato da ordem de mercado. Ao executar dados históricos, o modelo de execução real depende das configurações de backtesting em StockSharp.
- As informações de patrimônio e saldo são provenientes do portfólio conectado (`Portfolio.CurrentValue`, `Portfolio.CurrentBalance`). Garanta que o portfólio de estratégia esteja sincronizado com o título negociado.
- A cobertura abre posições de mercado adicionais no mesmo instrumento. Verifique se a corretora ou simulador permite posições opostas quando o hedge está habilitado.
- O rastreamento do ponto de equilíbrio reutiliza o valor `CloseProfitBuffer` assim como a versão original MetaTrader que operava com um parâmetro "ClosePL".
