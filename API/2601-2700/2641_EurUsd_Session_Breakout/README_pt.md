# Estratégia de Rompimento de Sessão EurUsd
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica a clássica ideia de rompimento do EUR/USD onde uma estreita faixa da manhã europeia é usada como trampolim para a sessão americana. Ela monitora uma janela deslizante de 24 velas (velas de 15 minutos por padrão) para medir a faixa de trading pré-EUA, filtra dias onde a faixa excede um limite configurável de pips, e então opera rompimentos que ocorrem completamente fora dessa banda. Apenas uma tentativa comprada e uma vendida são permitidas por dia de trading.

## Como Funciona

1. **Rastreamento de sessão** – no início da hora da sessão americana configurada, a estratégia bloqueia a faixa EU capturada pelas 24 velas completadas mais recentes (excluindo a barra atual). A faixa é ajustada automaticamente para valores de pips para cotações forex de 3 ou 5 dígitos.
2. **Filtro de faixa** – o trading é habilitado somente se a faixa EU capturada for menor que o limite *Sessão EU Pequena (pips)*.
3. **Validação de rompimento** – durante as horas de sessão americana permitidas, e somente entre `(hora início EU + 5)` e `(hora início EU + 10)`, a estratégia procura velas cujo corpo completo tenha operado fora da faixa armazenada com um buffer adicional medido em pontos.
4. **Execução de ordens** – uma compra a mercado é enviada quando a mínima da barra permanece acima da parte superior da faixa mais o buffer. Uma venda a mercado é enviada quando a máxima da barra permanece abaixo da parte inferior da faixa menos o buffer. Operações compradas e vendidas são sinalizadores independentes para que cada direção possa ser tentada uma vez por dia.
5. **Gestão de risco** – níveis de stop-loss e take-profit são definidos em pips, convertidos para distâncias de preço absolutas, e rastreados em cada vela finalizada usando extremos de máxima/mínima.

## Parâmetros

- **Início Sessão EU / Início Sessão US / Fim Sessão US** – horas (0–23) que definem quando o monitoramento EU começa e quando a janela de rompimento US está aberta.
- **Sessão EU Pequena (pips)** – tamanho máximo da faixa EU que ainda permite trading.
- **Operar na Segunda-feira** – habilita ou desabilita o trading nas segundas-feiras, bloqueando os fins de semana.
- **Stop-Loss (pips)** – distância entre o preço de entrada e o stop protetor, escalada automaticamente por tamanho de tick e dígitos.
- **Take-Profit (pips)** – distância do alvo de lucro, tratada da mesma forma que o stop.
- **Buffer de Rompimento (pontos)** – número de passos de preço adicionados ao gatilho de rompimento para que a barra confirmadora deva estar completamente além da faixa armazenada.
- **Tipo de Vela** – tipo de dado para a assinatura de velas; padrão é período de 15 minutos porque o script original foi projetado para gráficos M15.

## Notas Adicionais

- A estratégia assume contas de compensação: os níveis de proteção aplainam toda a posição usando ordens de mercado.
- O estado diário é reiniciado à meia-noite para que a faixa e os sinalizadores de rompimento não vazem entre sessões, enquanto posições abertas retêm seus alvos de preço.
- Como os níveis de stop-loss e take-profit são simulados com extremos de velas, picos intrabar que não aparecem nas barras históricas não serão detectados.
