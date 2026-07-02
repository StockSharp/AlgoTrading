# Estratégia cruzada tripla SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia de cruzamento triplo SMA replica o consultor especialista MQL original `3sma.mq4`. O sistema analisa três médias móveis simples (SMA) calculadas sobre o preço de fechamento e negocia quando a tendência de curto prazo se alinha com as médias de médio e longo prazo. A conversão mantém as regras de negociação originais enquanto as adapta à StockSharp estratégia de alto nível API.

## Lógica de negociação
1. Calcule três SMAs com períodos configuráveis.
2. Saia das posições longas existentes quando o SMA rápido cair abaixo do SMA médio.
3. Saia das posições curtas existentes quando o rápido SMA subir acima do médio SMA.
4. Insira uma nova posição longa quando:
   - O rápido SMA está acima do médio SMA em pelo menos o spread configurado.
   - O médio SMA está acima do lento SMA em pelo menos o spread configurado.
   - Nenhuma posição longa está aberta no momento.
5. Insira uma nova posição curta quando:
   - O rápido SMA está abaixo do médio SMA em pelo menos o spread configurado.
   - O médio SMA está abaixo do lento SMA em pelo menos o spread configurado.
   - Nenhuma posição curta está aberta no momento.

## Parâmetros
- **Tipo de vela** – Período principal usado para calcular as médias móveis.
- **Comprimento SMA rápido** – Período para o SMA rápido (MQL entrada `SMA1`).
- **Comprimento médio SMA** – Período para o meio SMA (entrada MQL `SMA2`).
- **Comprimento SMA lento** – Período para o SMA lento (entrada MQL `SMA3`).
- **SMA Etapas de spread** – Filtro adicional que exige que os SMAs diverjam em uma série de etapas de preço (MQL entrada `SMAspread`).
- **Volume de negociação** – Volume da ordem usado na abertura de posições (MQL entrada `lots`).

## Notas
- O tratamento de stop loss da versão MQL foi omitido porque foi desativado no script de origem.
- Todas as saídas são ordens de mercado alinhadas com o comportamento direto do especialista original.
