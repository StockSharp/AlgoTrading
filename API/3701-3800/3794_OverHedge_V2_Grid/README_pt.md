# Estratégia de grade OverHedge V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

OverHedge V2 é um sistema de grade de hedge que alterna posições longas e curtas enquanto aumenta o tamanho da negociação após cada preenchimento. A estratégia analisa a relação entre uma média móvel exponencial rápida e uma lenta (EMA) para decidir a direção dominante para o próximo ciclo. Assim que o ciclo começa, o algoritmo coloca ordens de mercado sempre que o preço atinge níveis de túnel predefinidos em torno da cotação inicial. A grade se expande simetricamente para que cada nova perna compense a perda flutuante da anterior. O ciclo termina quando o lucro aberto agregado excede uma meta configurável ou quando o trader solicita manualmente um encerramento.

A implementação mantém registros separados para exposição longa e curta e usa preços de nível 1 ao vivo para acionar novos hedges. O volume de negociação cresce geometricamente de acordo com o multiplicador escolhido, que reproduz o risco estilo martingale do consultor especialista MetaTrader original. Como as ordens são executadas a mercado, o sistema se adapta automaticamente às condições de liquidez, mantendo o espaçamento da grade expresso em pontos.

## Como funciona

1. **Filtro de direção** – A estratégia calcula duas EMAs em velas concluídas. Quando o EMA rápido está acima do EMA lento, o próximo ciclo começa com uma tendência longa; caso contrário, começa com um viés curto.
2. **Inicialização do ciclo** – No início de um ciclo, o algoritmo registra o preço de oferta atual e deriva dois limites de túnel separados pela largura configurada e pelo spread ao vivo. A primeira ordem segue o viés EMA, e a perna oposta é preparada na distância do túnel.
3. **Expansão da grade** – Se o preço continuar em relação à entrada mais recente, ordens de mercado adicionais serão acionadas alternadamente (compra, venda, compra,…). Cada nova perna multiplica o volume anterior pelo multiplicador de hedge, permitindo que a posição geral se recupere mais rapidamente em uma reversão.
4. **Coleta de lucros** – O ciclo monitora constantemente os lucros não realizados usando os melhores preços de compra/venda. Quando o valor alvo é atingido, ou se o operador alterna o sinalizador de desligamento, todas as pernas abertas são liquidadas e o ciclo é reiniciado.
5. **Acompanhamento de exposição** – A estratégia mantém o preço e o volume médios para hedges longos e curtos para calcular o lucro aberto com precisão e evitar o envio de pedidos duplicados enquanto os existentes ainda estão pendentes.

## Parâmetros padrão

- `Base Volume` = 0,1 lote – Tamanho inicial da negociação para a primeira etapa da grade.
- `Hedge Multiplier` = 2,0 – Multiplicador de volume aplicado a cada trecho subsequente.
- `Tunnel Width (points)` = 20 – Distância adicional entre ordens alternadas além do spread atual.
- `Profit Target` = 100 – Lucro não realizado na moeda da conta que fecha toda a grade.
- `Short EMA` = 8 – Período do EMA rápida usado para detecção de direção.
- `Long EMA` = 21 – Período de lentidão EMA usado para detecção de direção.
- `Candle Type` = 1 minuto – Período que alimenta os filtros EMA.
- `Shutdown Grid` = falso – Quando verdadeiro, a estratégia sai imediatamente de todas as etapas e para de negociar.

## Notas

- A grade funciona com qualquer instrumento que forneça cotações de Nível 1 (melhor compra/venda). Spreads mais amplos aumentam automaticamente o tamanho do túnel.
- O volume de negociação é normalizado usando a etapa de volume de segurança para evitar pedidos rejeitados.
- Como o sistema utiliza um esquema de dimensionamento martingale, grandes rebaixamentos são possíveis se as tendências de preços persistirem sem atingir a meta de lucro.
- Para retomar a negociação após um encerramento, alterne o parâmetro de volta para `false` ou reinicie a estratégia.
